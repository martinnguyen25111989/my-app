import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ImportService, WalletService } from '../../core/api.services';
import { ImportResult, Wallet } from '../../core/models';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [
    FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatSelectModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  template: `
    <div class="page-container">
      <h1>Import sao kê ngân hàng</h1>
      <mat-card>
        <mat-card-content>
          <p>Tải lên file sao kê <b>.csv</b> hoặc <b>.xlsx</b> gồm 3 cột:
            <code>Ngày (dd/MM/yyyy)</code>, <code>Mô tả</code>, <code>Số tiền</code>
            (dương = thu, âm = chi). Mỗi giao dịch chi sẽ được <b>AI phân loại tự động</b> vào danh mục phù hợp.</p>

          <div class="filter-row">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Import vào ví</mat-label>
              <mat-select [(ngModel)]="walletId">
                @for (w of wallets(); track w.id) {
                  <mat-option [value]="w.id">{{ w.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <button mat-stroked-button (click)="fileInput.click()">
              <mat-icon>attach_file</mat-icon> {{ file?.name ?? 'Chọn file' }}
            </button>
            <input #fileInput type="file" hidden accept=".csv,.xlsx,.xls" (change)="onFileSelected($event)">

            <button mat-flat-button (click)="upload()"
                    [disabled]="!file || !walletId || uploading()">
              @if (uploading()) { <mat-spinner diameter="20" /> } @else { <ng-container><mat-icon>upload</mat-icon> Import</ng-container> }
            </button>
          </div>

          @if (result(); as r) {
            <div class="result">
              <p class="income">✅ Đã import {{ r.imported }} giao dịch.</p>
              @if (r.skipped > 0) { <p>⏭️ Bỏ qua {{ r.skipped }} dòng không hợp lệ.</p> }
              @for (error of r.errors; track $index) { <p class="expense">{{ error }}</p> }
            </div>
          }
        </mat-card-content>
      </mat-card>

      <mat-card style="margin-top: 16px;">
        <mat-card-header><mat-card-title>Ví dụ file CSV</mat-card-title></mat-card-header>
        <mat-card-content>
          <pre>Date,Description,Amount
10/06/2026,GRABFOOD don hang com trua,-85000
09/06/2026,Luong thang 6,25000000
08/06/2026,EVN tien dien,-450000</pre>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    pre { background: rgba(128,128,128,0.1); padding: 12px; border-radius: 4px; overflow-x: auto; }
    .result { margin-top: 16px; }
  `]
})
export class ImportComponent implements OnInit {
  private importService = inject(ImportService);
  private walletService = inject(WalletService);
  private snackBar = inject(MatSnackBar);

  wallets = signal<Wallet[]>([]);
  result = signal<ImportResult | null>(null);
  uploading = signal(false);
  walletId: string | null = null;
  file: File | null = null;

  ngOnInit(): void {
    this.walletService.list().subscribe(wallets => {
      this.wallets.set(wallets);
      this.walletId ??= wallets[0]?.id ?? null;
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0] ?? null;
  }

  upload(): void {
    if (!this.file || !this.walletId) return;
    this.uploading.set(true);
    this.result.set(null);
    this.importService.importBankStatement(this.file, this.walletId).subscribe({
      next: result => {
        this.uploading.set(false);
        this.result.set(result);
        this.file = null;
      },
      error: err => {
        this.uploading.set(false);
        this.snackBar.open(err.error?.message ?? 'Import thất bại.', 'Đóng', { duration: 5000 });
      }
    });
  }
}
