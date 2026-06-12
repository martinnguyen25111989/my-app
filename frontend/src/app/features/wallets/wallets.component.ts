import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { WalletService } from '../../core/api.services';
import { WALLET_TYPE_LABELS, Wallet, WalletType } from '../../core/models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

const WALLET_ICONS: Record<number, string> = {
  [WalletType.Cash]: 'payments',
  [WalletType.Bank]: 'account_balance',
  [WalletType.Momo]: 'smartphone',
  [WalletType.ZaloPay]: 'smartphone',
  [WalletType.CreditCard]: 'credit_card',
  [WalletType.Other]: 'account_balance_wallet'
};

// ===== Dialog thêm/sửa ví =====
@Component({
  selector: 'app-wallet-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Sửa ví' : 'Thêm ví' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tên ví</mat-label>
          <input matInput formControlName="name" maxlength="100">
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Loại ví</mat-label>
          <mat-select formControlName="type">
            @for (entry of walletTypes; track entry[0]) {
              <mat-option [value]="+entry[0]">{{ entry[1] }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Số dư ban đầu</mat-label>
          <input matInput formControlName="initialBalance" type="number" min="0">
          <span matSuffix>₫</span>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Hủy</button>
      <button mat-flat-button (click)="save()" [disabled]="form.invalid">Lưu</button>
    </mat-dialog-actions>
  `,
  styles: ['.dialog-form { display: flex; flex-direction: column; gap: 8px; min-width: 300px; padding-top: 8px; }']
})
export class WalletDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<WalletDialogComponent>);
  data = inject<Wallet | undefined>(MAT_DIALOG_DATA);

  walletTypes = Object.entries(WALLET_TYPE_LABELS);

  form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    type: [this.data?.type ?? WalletType.Cash, Validators.required],
    initialBalance: [this.data?.initialBalance ?? 0, [Validators.required, Validators.min(0)]],
    currency: [this.data?.currency ?? 'VND']
  });

  save(): void {
    if (this.form.valid) this.dialogRef.close(this.form.getRawValue());
  }
}

// ===== Trang ví tiền =====
@Component({
  selector: 'app-wallets',
  standalone: true,
  imports: [
    CurrencyPipe, MatCardModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatSnackBarModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Ví tiền</h1>
        <span class="spacer"></span>
        <button mat-flat-button (click)="openDialog()">
          <mat-icon>add</mat-icon> Thêm ví
        </button>
      </div>

      <div class="summary-cards">
        @for (wallet of wallets(); track wallet.id) {
          <mat-card>
            <mat-card-content>
              <div class="wallet-header">
                <mat-icon color="primary">{{ walletIcons[wallet.type] }}</mat-icon>
                <div>
                  <div class="wallet-name">{{ wallet.name }}</div>
                  <small>{{ walletTypeLabels[wallet.type] }}</small>
                </div>
                <span class="spacer"></span>
                <button mat-icon-button (click)="openDialog(wallet)"><mat-icon>edit</mat-icon></button>
                <button mat-icon-button color="warn" (click)="remove(wallet)"><mat-icon>delete</mat-icon></button>
              </div>
              <div class="wallet-balance" [class.expense]="wallet.currentBalance < 0">
                {{ wallet.currentBalance | currency:'VND':'symbol':'1.0-0' }}
              </div>
              <small>Số dư ban đầu: {{ wallet.initialBalance | currency:'VND':'symbol':'1.0-0' }}</small>
            </mat-card-content>
          </mat-card>
        } @empty {
          <p>Chưa có ví nào. Hãy thêm ví đầu tiên!</p>
        }
      </div>
    </div>
  `,
  styles: [`
    h1 { margin: 0; }
    .wallet-header { display: flex; align-items: center; gap: 12px; }
    .wallet-name { font-weight: 500; font-size: 16px; }
    .wallet-balance { font-size: 24px; font-weight: 600; margin: 12px 0 4px; }
    small { color: gray; }
  `]
})
export class WalletsComponent implements OnInit {
  private walletService = inject(WalletService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  walletTypeLabels = WALLET_TYPE_LABELS;
  walletIcons = WALLET_ICONS;
  wallets = signal<Wallet[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.walletService.list().subscribe(wallets => this.wallets.set(wallets));
  }

  openDialog(wallet?: Wallet): void {
    this.dialog.open(WalletDialogComponent, { data: wallet }).afterClosed().subscribe(value => {
      if (!value) return;
      const request = wallet
        ? this.walletService.update(wallet.id, value)
        : this.walletService.create(value);
      request.subscribe({
        next: () => this.load(),
        error: err => this.snackBar.open(err.error?.message ?? 'Lưu thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }

  remove(wallet: Wallet): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Xóa ví', message: `Xóa ví "${wallet.name}"?` }
    }).afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.walletService.delete(wallet.id).subscribe({
        next: () => this.load(),
        error: err => this.snackBar.open(err.error?.message ?? 'Xóa thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }
}
