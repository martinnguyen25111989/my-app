import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BudgetService, CategoryService } from '../../core/api.services';
import { Budget, Category, TransactionType } from '../../core/models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

export interface BudgetDialogData {
  budget?: Budget;
  categories: Category[];
  month: number;
  year: number;
}

// ===== Dialog thêm/sửa ngân sách =====
@Component({
  selector: 'app-budget-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.budget ? 'Sửa ngân sách' : 'Thêm ngân sách' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Danh mục chi tiêu</mat-label>
          <mat-select formControlName="categoryId">
            @for (c of data.categories; track c.id) {
              <mat-option [value]="c.id">{{ c.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Hạn mức tháng {{ data.month }}/{{ data.year }}</mat-label>
          <input matInput formControlName="amount" type="number" min="0">
          <span matSuffix>₫</span>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Hủy</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="form.invalid">Lưu</button>
    </mat-dialog-actions>
  `,
  styles: ['.dialog-form { display: flex; flex-direction: column; gap: 8px; min-width: 300px; padding-top: 8px; }']
})
export class BudgetDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<BudgetDialogComponent>);
  data = inject<BudgetDialogData>(MAT_DIALOG_DATA);

  form = this.fb.nonNullable.group({
    categoryId: [{ value: this.data.budget?.categoryId ?? '', disabled: !!this.data.budget }, Validators.required],
    amount: [this.data.budget?.amount ?? 0, [Validators.required, Validators.min(1)]]
  });

  save(): void {
    if (this.form.valid) this.dialogRef.close(this.form.getRawValue());
  }
}

// ===== Trang ngân sách =====
@Component({
  selector: 'app-budgets',
  standalone: true,
  imports: [
    CurrencyPipe, FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatSelectModule, MatProgressBarModule, MatDialogModule, MatSnackBarModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Ngân sách</h1>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Tháng</mat-label>
          <mat-select [(ngModel)]="month" (selectionChange)="load()">
            @for (m of months; track m) { <mat-option [value]="m">Tháng {{ m }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Năm</mat-label>
          <mat-select [(ngModel)]="year" (selectionChange)="load()">
            @for (y of years; track y) { <mat-option [value]="y">{{ y }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <button mat-raised-button color="primary" (click)="openDialog()">
          <mat-icon>add</mat-icon> Thêm ngân sách
        </button>
      </div>

      <div class="summary-cards">
        @for (budget of budgets(); track budget.id) {
          <mat-card>
            <mat-card-content>
              <div class="budget-header">
                <span class="budget-name" [style.color]="budget.categoryColor">{{ budget.categoryName }}</span>
                <span class="spacer"></span>
                <button mat-icon-button (click)="openDialog(budget)"><mat-icon>edit</mat-icon></button>
                <button mat-icon-button color="warn" (click)="remove(budget)"><mat-icon>delete</mat-icon></button>
              </div>
              <mat-progress-bar [mode]="'determinate'" [value]="Math.min(100, budget.percentage)"
                                [color]="budget.percentage >= 100 ? 'warn' : budget.percentage >= 80 ? 'accent' : 'primary'" />
              <div class="budget-detail">
                <span>Đã chi: <b [class.expense]="budget.percentage >= 100">
                  {{ budget.spent | currency:'VND':'symbol':'1.0-0' }}</b> ({{ budget.percentage }}%)</span>
                <span>Hạn mức: {{ budget.amount | currency:'VND':'symbol':'1.0-0' }}</span>
              </div>
              @if (budget.percentage >= 100) {
                <p class="alert exceeded">⚠️ Đã vượt ngân sách!</p>
              } @else if (budget.percentage >= 80) {
                <p class="alert warning">⚠️ Sắp chạm hạn mức ({{ budget.percentage }}%)</p>
              }
            </mat-card-content>
          </mat-card>
        } @empty {
          <p>Chưa có ngân sách nào cho tháng {{ month }}/{{ year }}.</p>
        }
      </div>
    </div>
  `,
  styles: [`
    h1 { margin: 0; }
    .budget-header { display: flex; align-items: center; margin-bottom: 8px; }
    .budget-name { font-weight: 600; font-size: 16px; }
    .budget-detail { display: flex; justify-content: space-between; margin-top: 8px; font-size: 14px; }
    .alert { margin: 8px 0 0; font-size: 13px; }
    .alert.warning { color: #e65100; }
    .alert.exceeded { color: #b71c1c; }
  `]
})
export class BudgetsComponent implements OnInit {
  private budgetService = inject(BudgetService);
  private categoryService = inject(CategoryService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  Math = Math;
  months = Array.from({ length: 12 }, (_, i) => i + 1);
  years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i + 1);
  month = new Date().getMonth() + 1;
  year = new Date().getFullYear();

  budgets = signal<Budget[]>([]);
  expenseCategories = signal<Category[]>([]);

  ngOnInit(): void {
    this.categoryService.list(undefined, TransactionType.Expense)
      .subscribe(result => this.expenseCategories.set(result.items));
    this.load();
  }

  load(): void {
    this.budgetService.list(this.month, this.year).subscribe(budgets => this.budgets.set(budgets));
  }

  openDialog(budget?: Budget): void {
    this.dialog.open(BudgetDialogComponent, {
      data: { budget, categories: this.expenseCategories(), month: this.month, year: this.year }
    }).afterClosed().subscribe(value => {
      if (!value) return;
      const request = budget
        ? this.budgetService.update(budget.id, value.amount)
        : this.budgetService.create({ ...value, month: this.month, year: this.year });
      request.subscribe({
        next: () => this.load(),
        error: err => this.snackBar.open(err.error?.message ?? 'Lưu thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }

  remove(budget: Budget): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Xóa ngân sách', message: `Xóa ngân sách "${budget.categoryName}"?` }
    }).afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.budgetService.delete(budget.id).subscribe(() => this.load());
    });
  }
}
