import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { ReportService } from '../../core/api.services';
import { Report, TransactionType } from '../../core/models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CurrencyPipe, DatePipe, FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatTableModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Báo cáo</h1>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Từ ngày</mat-label>
          <input matInput [matDatepicker]="fromPicker" [(ngModel)]="fromDate">
          <mat-datepicker-toggle matIconSuffix [for]="fromPicker" />
          <mat-datepicker #fromPicker />
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Đến ngày</mat-label>
          <input matInput [matDatepicker]="toPicker" [(ngModel)]="toDate">
          <mat-datepicker-toggle matIconSuffix [for]="toPicker" />
          <mat-datepicker #toPicker />
        </mat-form-field>
        <button mat-raised-button color="primary" (click)="load()">Xem báo cáo</button>
        <button mat-stroked-button (click)="exportFile('excel')" [disabled]="!report()">
          <mat-icon>table_view</mat-icon> Excel
        </button>
        <button mat-stroked-button (click)="exportFile('pdf')" [disabled]="!report()">
          <mat-icon>picture_as_pdf</mat-icon> PDF
        </button>
      </div>

      @if (report(); as r) {
        <div class="summary-cards">
          <mat-card><mat-card-content>
            <div class="label">Tổng thu</div>
            <div class="value income">{{ r.totalIncome | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content></mat-card>
          <mat-card><mat-card-content>
            <div class="label">Tổng chi</div>
            <div class="value expense">{{ r.totalExpense | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content></mat-card>
          <mat-card><mat-card-content>
            <div class="label">Chênh lệch</div>
            <div class="value">{{ r.balance | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content></mat-card>
        </div>

        <mat-card class="table-card">
          <mat-card-header><mat-card-title>Giao dịch trong kỳ ({{ r.transactions.length }})</mat-card-title></mat-card-header>
          <mat-card-content>
            <table mat-table [dataSource]="r.transactions">
              <ng-container matColumnDef="date">
                <th mat-header-cell *matHeaderCellDef>Ngày</th>
                <td mat-cell *matCellDef="let t">{{ t.transactionDate | date:'dd/MM/yyyy' }}</td>
              </ng-container>
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Danh mục</th>
                <td mat-cell *matCellDef="let t">{{ t.categoryName }}</td>
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
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns;"></tr>
            </table>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    h1 { margin: 0; }
    .label { color: gray; font-size: 14px; }
    .value { font-size: 24px; font-weight: 600; margin-top: 8px; }
  `]
})
export class ReportsComponent implements OnInit {
  private reportService = inject(ReportService);

  TransactionType = TransactionType;
  columns = ['date', 'category', 'note', 'amount'];

  fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
  toDate = new Date();
  report = signal<Report | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.reportService.get(toIso(this.fromDate), toIso(this.toDate))
      .subscribe(report => this.report.set(report));
  }

  exportFile(format: 'excel' | 'pdf'): void {
    this.reportService.exportFile(format, toIso(this.fromDate), toIso(this.toDate)).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `bao-cao-thu-chi.${format === 'excel' ? 'xlsx' : 'pdf'}`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }
}

function toIso(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
