import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CategoryService } from '../../core/api.services';
import { Category, TransactionType } from '../../core/models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

const ICONS = ['category', 'restaurant', 'directions_car', 'shopping_cart', 'receipt_long',
  'movie', 'medical_services', 'school', 'payments', 'card_giftcard', 'savings',
  'home', 'pets', 'flight', 'fitness_center', 'child_care', 'more_horiz'];

// ===== Dialog thêm/sửa danh mục =====
@Component({
  selector: 'app-category-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatButtonToggleModule, MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Sửa danh mục' : 'Thêm danh mục' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-button-toggle-group formControlName="type">
          <mat-button-toggle [value]="TransactionType.Expense">Chi tiêu</mat-button-toggle>
          <mat-button-toggle [value]="TransactionType.Income">Thu nhập</mat-button-toggle>
        </mat-button-toggle-group>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tên danh mục</mat-label>
          <input matInput formControlName="name" maxlength="100">
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Biểu tượng</mat-label>
          <mat-select formControlName="icon">
            @for (icon of icons; track icon) {
              <mat-option [value]="icon"><mat-icon>{{ icon }}</mat-icon> {{ icon }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <label>Màu sắc:
          <input type="color" formControlName="color" style="margin-left: 8px;">
        </label>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Hủy</button>
      <button mat-flat-button (click)="save()" [disabled]="form.invalid">Lưu</button>
    </mat-dialog-actions>
  `,
  styles: ['.dialog-form { display: flex; flex-direction: column; gap: 12px; min-width: 300px; padding-top: 8px; }']
})
export class CategoryDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<CategoryDialogComponent>);
  data = inject<Category | undefined>(MAT_DIALOG_DATA);

  TransactionType = TransactionType;
  icons = ICONS;

  form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    type: [this.data?.type ?? TransactionType.Expense, Validators.required],
    icon: [this.data?.icon ?? 'category', Validators.required],
    color: [this.data?.color ?? '#1976d2', Validators.required]
  });

  save(): void {
    if (this.form.valid) this.dialogRef.close(this.form.getRawValue());
  }
}

// ===== Trang danh mục =====
@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    FormsModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDialogModule, MatSnackBarModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Danh mục thu chi</h1>
        <span class="spacer"></span>
        <button mat-flat-button (click)="openDialog()">
          <mat-icon>add</mat-icon> Thêm danh mục
        </button>
      </div>

      <mat-card class="table-card">
        <mat-card-content>
          <div class="filter-row">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Tìm kiếm</mat-label>
              <input matInput [(ngModel)]="search" (keyup.enter)="load()">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Loại</mat-label>
              <mat-select [(ngModel)]="type" (selectionChange)="load()">
                <mat-option [value]="null">Tất cả</mat-option>
                <mat-option [value]="TransactionType.Income">Thu nhập</mat-option>
                <mat-option [value]="TransactionType.Expense">Chi tiêu</mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          <table mat-table [dataSource]="categories()">
            <ng-container matColumnDef="icon">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let c">
                <mat-icon [style.color]="c.color">{{ c.icon }}</mat-icon>
              </td>
            </ng-container>
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Tên</th>
              <td mat-cell *matCellDef="let c">{{ c.name }}</td>
            </ng-container>
            <ng-container matColumnDef="type">
              <th mat-header-cell *matHeaderCellDef>Loại</th>
              <td mat-cell *matCellDef="let c">
                <span [class]="c.type === TransactionType.Income ? 'income' : 'expense'">
                  {{ c.type === TransactionType.Income ? 'Thu nhập' : 'Chi tiêu' }}
                </span>
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let c" style="text-align: right; white-space: nowrap;">
                <button mat-icon-button (click)="openDialog(c)"><mat-icon>edit</mat-icon></button>
                <button mat-icon-button color="warn" (click)="remove(c)"><mat-icon>delete</mat-icon></button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns;"></tr>
            <tr class="mat-row" *matNoDataRow>
              <td class="mat-cell" [attr.colspan]="columns.length" style="padding: 24px; text-align: center;">
                Không tìm thấy danh mục.
              </td>
            </tr>
          </table>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: ['h1 { margin: 0; }']
})
export class CategoriesComponent implements OnInit {
  private categoryService = inject(CategoryService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  TransactionType = TransactionType;
  columns = ['icon', 'name', 'type', 'actions'];
  categories = signal<Category[]>([]);
  search = '';
  type: TransactionType | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.categoryService.list(this.search || undefined, this.type ?? undefined)
      .subscribe(result => this.categories.set(result.items));
  }

  openDialog(category?: Category): void {
    this.dialog.open(CategoryDialogComponent, { data: category }).afterClosed().subscribe(value => {
      if (!value) return;
      const request = category
        ? this.categoryService.update(category.id, value)
        : this.categoryService.create(value);
      request.subscribe({
        next: () => this.load(),
        error: err => this.snackBar.open(err.error?.message ?? 'Lưu thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }

  remove(category: Category): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Xóa danh mục', message: `Xóa danh mục "${category.name}"?` }
    }).afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.categoryService.delete(category.id).subscribe({
        next: () => this.load(),
        error: err => this.snackBar.open(err.error?.message ?? 'Xóa thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }
}
