import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AiService, TransactionService } from '../../core/api.services';
import { Category, Transaction, TransactionType, Wallet } from '../../core/models';

export interface TransactionDialogData {
  transaction?: Transaction;
  categories: Category[];
  wallets: Wallet[];
}

@Component({
  selector: 'app-transaction-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatButtonToggleModule, MatDatepickerModule,
    MatNativeDateModule, MatIconModule, MatSnackBarModule, MatTooltipModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.transaction ? 'Sửa giao dịch' : 'Thêm giao dịch' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-button-toggle-group formControlName="type" class="full-width">
          <mat-button-toggle [value]="TransactionType.Expense">Chi tiêu</mat-button-toggle>
          <mat-button-toggle [value]="TransactionType.Income">Thu nhập</mat-button-toggle>
        </mat-button-toggle-group>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Số tiền</mat-label>
          <input matInput formControlName="amount" type="number" min="0">
          <span matSuffix>₫</span>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Ghi chú</mat-label>
          <input matInput formControlName="note" maxlength="500">
          @if (form.value.type === TransactionType.Expense) {
            <button mat-icon-button matSuffix type="button" (click)="classify()"
                    matTooltip="AI gợi ý danh mục từ ghi chú" [disabled]="classifying()">
              <mat-icon>auto_awesome</mat-icon>
            </button>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Danh mục</mat-label>
          <mat-select formControlName="categoryId">
            @for (c of filteredCategories(); track c.id) {
              <mat-option [value]="c.id">{{ c.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Ví</mat-label>
          <mat-select formControlName="walletId">
            @for (w of data.wallets; track w.id) {
              <mat-option [value]="w.id">{{ w.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Ngày giao dịch</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="transactionDate">
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Hủy</button>
      <button mat-flat-button (click)="save()" [disabled]="form.invalid || saving()">
        Lưu
      </button>
    </mat-dialog-actions>
  `,
  styles: ['.dialog-form { display: flex; flex-direction: column; gap: 8px; min-width: 320px; padding-top: 8px; }']
})
export class TransactionDialogComponent {
  private fb = inject(FormBuilder);
  private transactionService = inject(TransactionService);
  private aiService = inject(AiService);
  private snackBar = inject(MatSnackBar);
  private dialogRef = inject(MatDialogRef<TransactionDialogComponent>);
  data = inject<TransactionDialogData>(MAT_DIALOG_DATA);

  TransactionType = TransactionType;
  saving = signal(false);
  classifying = signal(false);

  form = this.fb.nonNullable.group({
    type: [this.data.transaction?.type ?? TransactionType.Expense, Validators.required],
    amount: [this.data.transaction?.amount ?? 0, [Validators.required, Validators.min(1)]],
    note: [this.data.transaction?.note ?? ''],
    categoryId: [this.data.transaction?.categoryId ?? '', Validators.required],
    walletId: [this.data.transaction?.walletId ?? this.data.wallets[0]?.id ?? '', Validators.required],
    transactionDate: [this.data.transaction ? new Date(this.data.transaction.transactionDate) : new Date(), Validators.required]
  });

  filteredCategories(): Category[] {
    return this.data.categories.filter(c => c.type === this.form.value.type);
  }

  classify(): void {
    const note = this.form.value.note;
    if (!note) {
      this.snackBar.open('Nhập ghi chú trước để AI phân loại.', 'Đóng', { duration: 3000 });
      return;
    }
    this.classifying.set(true);
    this.aiService.classify(note).subscribe({
      next: result => {
        this.classifying.set(false);
        if (result.categoryId) {
          this.form.patchValue({ categoryId: result.categoryId });
          this.snackBar.open(`AI gợi ý: ${result.categoryName}`, 'Đóng', { duration: 3000 });
        } else {
          this.snackBar.open('AI không tìm được danh mục phù hợp.', 'Đóng', { duration: 3000 });
        }
      },
      error: () => this.classifying.set(false)
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();
    const payload = {
      ...value,
      note: value.note || null,
      transactionDate: formatDate(value.transactionDate)
    };

    const request = this.data.transaction
      ? this.transactionService.update(this.data.transaction.id, payload)
      : this.transactionService.create(payload);

    request.subscribe({
      next: () => this.dialogRef.close(true),
      error: err => {
        this.saving.set(false);
        this.snackBar.open(err.error?.message ?? 'Lưu thất bại.', 'Đóng', { duration: 4000 });
      }
    });
  }
}

function formatDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
