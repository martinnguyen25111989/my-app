import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CategoryService, TransactionService, WalletService } from '../../core/api.services';
import { Category, PagedResult, Transaction, TransactionType, Wallet } from '../../core/models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { TransactionDialogComponent } from './transaction-dialog.component';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [
    CurrencyPipe, DatePipe, FormsModule, MatCardModule, MatTableModule, MatPaginatorModule,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatDialogModule, MatSnackBarModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Giao dịch</h1>
        <span class="spacer"></span>
        <button mat-flat-button (click)="openDialog()">
          <mat-icon>add</mat-icon> Thêm giao dịch
        </button>
      </div>

      <mat-card class="table-card">
        <mat-card-content>
          <div class="filter-row">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Tìm theo ghi chú</mat-label>
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
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Danh mục</mat-label>
              <mat-select [(ngModel)]="categoryId" (selectionChange)="load()">
                <mat-option [value]="null">Tất cả</mat-option>
                @for (c of categories(); track c.id) {
                  <mat-option [value]="c.id">{{ c.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Ví</mat-label>
              <mat-select [(ngModel)]="walletId" (selectionChange)="load()">
                <mat-option [value]="null">Tất cả</mat-option>
                @for (w of wallets(); track w.id) {
                  <mat-option [value]="w.id">{{ w.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>

          <table mat-table [dataSource]="result()?.items ?? []">
            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Ngày</th>
              <td mat-cell *matCellDef="let t">{{ t.transactionDate | date:'dd/MM/yyyy' }}</td>
            </ng-container>
            <ng-container matColumnDef="category">
              <th mat-header-cell *matHeaderCellDef>Danh mục</th>
              <td mat-cell *matCellDef="let t">
                <span class="category-chip" [style.background]="t.categoryColor + '22'"
                      [style.color]="t.categoryColor">
                  <mat-icon inline>{{ t.categoryIcon }}</mat-icon> {{ t.categoryName }}
                </span>
              </td>
            </ng-container>
            <ng-container matColumnDef="wallet">
              <th mat-header-cell *matHeaderCellDef>Ví</th>
              <td mat-cell *matCellDef="let t">{{ t.walletName }}</td>
            </ng-container>
            <ng-container matColumnDef="note">
              <th mat-header-cell *matHeaderCellDef>Ghi chú</th>
              <td mat-cell *matCellDef="let t">{{ t.note }}</td>
            </ng-container>
            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef style="text-align: right;">Số tiền</th>
              <td mat-cell *matCellDef="let t" style="text-align: right;"
                  [class]="t.type === TransactionType.Income ? 'income' : 'expense'">
                {{ (t.type === TransactionType.Income ? '+' : '-') }}{{ t.amount | currency:'VND':'symbol':'1.0-0' }}
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let t" style="text-align: right; white-space: nowrap;">
                <button mat-icon-button (click)="openDialog(t)"><mat-icon>edit</mat-icon></button>
                <button mat-icon-button color="warn" (click)="remove(t)"><mat-icon>delete</mat-icon></button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns;"></tr>
            <tr class="mat-row" *matNoDataRow>
              <td class="mat-cell" [attr.colspan]="columns.length" style="padding: 24px; text-align: center;">
                Chưa có giao dịch nào.
              </td>
            </tr>
          </table>

          <mat-paginator [length]="result()?.totalCount ?? 0" [pageSize]="pageSize"
                         [pageIndex]="page - 1" [pageSizeOptions]="[10, 20, 50]"
                         (page)="onPage($event)" showFirstLastButtons />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    h1 { margin: 0; }
    .category-chip {
      display: inline-flex; align-items: center; gap: 4px;
      padding: 2px 10px; border-radius: 12px; font-size: 13px;
    }
  `]
})
export class TransactionsComponent implements OnInit {
  private transactionService = inject(TransactionService);
  private categoryService = inject(CategoryService);
  private walletService = inject(WalletService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  TransactionType = TransactionType;
  columns = ['date', 'category', 'wallet', 'note', 'amount', 'actions'];

  result = signal<PagedResult<Transaction> | null>(null);
  categories = signal<Category[]>([]);
  wallets = signal<Wallet[]>([]);

  search = '';
  type: TransactionType | null = null;
  categoryId: string | null = null;
  walletId: string | null = null;
  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    this.categoryService.list().subscribe(r => this.categories.set(r.items));
    this.walletService.list().subscribe(w => this.wallets.set(w));
    this.load();
  }

  load(): void {
    this.transactionService.list({
      search: this.search || undefined,
      type: this.type ?? undefined,
      categoryId: this.categoryId ?? undefined,
      walletId: this.walletId ?? undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe(result => this.result.set(result));
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  openDialog(transaction?: Transaction): void {
    this.dialog.open(TransactionDialogComponent, {
      data: { transaction, categories: this.categories(), wallets: this.wallets() }
    }).afterClosed().subscribe(saved => saved && this.load());
  }

  remove(transaction: Transaction): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Xóa giao dịch', message: 'Bạn có chắc muốn xóa giao dịch này?' }
    }).afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.transactionService.delete(transaction.id).subscribe({
        next: () => {
          this.snackBar.open('Đã xóa giao dịch.', 'Đóng', { duration: 3000 });
          this.load();
        },
        error: err => this.snackBar.open(err.error?.message ?? 'Xóa thất bại.', 'Đóng', { duration: 4000 })
      });
    });
  }
}
