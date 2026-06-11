import { CurrencyPipe } from '@angular/common';
import {
  AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Chart, registerables } from 'chart.js';
import { AiService, DashboardService } from '../../core/api.services';
import { Dashboard, WALLET_TYPE_LABELS } from '../../core/models';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CurrencyPipe, FormsModule, MatCardModule, MatIconModule, MatButtonModule,
    MatFormFieldModule, MatSelectModule, MatListModule, MatProgressBarModule
  ],
  template: `
    <div class="page-container">
      <div class="filter-row">
        <h1>Tổng quan</h1>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Tháng</mat-label>
          <mat-select [(ngModel)]="month" (selectionChange)="load()">
            @for (m of months; track m) {
              <mat-option [value]="m">Tháng {{ m }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Năm</mat-label>
          <mat-select [(ngModel)]="year" (selectionChange)="load()">
            @for (y of years; track y) {
              <mat-option [value]="y">{{ y }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </div>

      @for (alert of data()?.budgetAlerts ?? []; track alert.budgetId) {
        <div class="budget-alert" [class.exceeded]="alert.level === 'Exceeded'">
          <mat-icon>{{ alert.level === 'Exceeded' ? 'error' : 'warning' }}</mat-icon>
          <span>{{ alert.message }}</span>
        </div>
      }

      <div class="summary-cards">
        <mat-card>
          <mat-card-content>
            <div class="summary-label"><mat-icon class="income">trending_up</mat-icon> Tổng thu tháng</div>
            <div class="summary-value income">{{ data()?.totalIncome | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content>
        </mat-card>
        <mat-card>
          <mat-card-content>
            <div class="summary-label"><mat-icon class="expense">trending_down</mat-icon> Tổng chi tháng</div>
            <div class="summary-value expense">{{ data()?.totalExpense | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content>
        </mat-card>
        <mat-card>
          <mat-card-content>
            <div class="summary-label"><mat-icon>account_balance</mat-icon> Số dư hiện tại</div>
            <div class="summary-value">{{ data()?.balance | currency:'VND':'symbol':'1.0-0' }}</div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="chart-grid">
        <mat-card>
          <mat-card-header><mat-card-title>Thu chi 6 tháng gần nhất</mat-card-title></mat-card-header>
          <mat-card-content><canvas #barChart></canvas></mat-card-content>
        </mat-card>
        <mat-card>
          <mat-card-header><mat-card-title>Top danh mục chi tiêu</mat-card-title></mat-card-header>
          <mat-card-content>
            @if ((data()?.topExpenseCategories ?? []).length === 0) {
              <p>Chưa có chi tiêu trong tháng.</p>
            }
            <canvas #doughnutChart></canvas>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="chart-grid" style="margin-top: 16px;">
        <mat-card>
          <mat-card-header>
            <mat-card-title><mat-icon style="vertical-align: middle;">auto_awesome</mat-icon> Phân tích AI</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <mat-list>
              @for (insight of insights(); track $index) {
                <mat-list-item lines="2">
                  <mat-icon matListItemIcon>insights</mat-icon>
                  <span matListItemTitle style="white-space: normal;">{{ insight }}</span>
                </mat-list-item>
              }
            </mat-list>
          </mat-card-content>
        </mat-card>
        <mat-card>
          <mat-card-header><mat-card-title>Ví tiền</mat-card-title></mat-card-header>
          <mat-card-content>
            <mat-list>
              @for (wallet of data()?.wallets ?? []; track wallet.id) {
                <mat-list-item>
                  <mat-icon matListItemIcon>account_balance_wallet</mat-icon>
                  <span matListItemTitle>{{ wallet.name }} ({{ walletTypeLabels[wallet.type] }})</span>
                  <span matListItemLine [class.expense]="wallet.currentBalance < 0">
                    {{ wallet.currentBalance | currency:'VND':'symbol':'1.0-0' }}
                  </span>
                </mat-list-item>
              }
            </mat-list>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .summary-label { display: flex; align-items: center; gap: 8px; color: gray; font-size: 14px; }
    .summary-value { font-size: 26px; font-weight: 600; margin-top: 8px; }
    .budget-alert {
      display: flex; align-items: center; gap: 8px;
      background: #fff3e0; color: #e65100; border-left: 4px solid #fb8c00;
      padding: 10px 16px; border-radius: 4px; margin-bottom: 12px;
    }
    .budget-alert.exceeded { background: #ffebee; color: #b71c1c; border-color: #c62828; }
    h1 { margin: 0; }
  `]
})
export class DashboardComponent implements AfterViewInit, OnDestroy {
  private dashboardService = inject(DashboardService);
  private aiService = inject(AiService);

  @ViewChild('barChart') barChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('doughnutChart') doughnutChartRef!: ElementRef<HTMLCanvasElement>;
  private barChart?: Chart;
  private doughnutChart?: Chart;

  walletTypeLabels = WALLET_TYPE_LABELS;
  months = Array.from({ length: 12 }, (_, i) => i + 1);
  years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i);
  month = new Date().getMonth() + 1;
  year = new Date().getFullYear();

  data = signal<Dashboard | null>(null);
  insights = signal<string[]>([]);

  ngAfterViewInit(): void {
    this.load();
  }

  load(): void {
    this.dashboardService.get(this.month, this.year).subscribe(data => {
      this.data.set(data);
      this.renderCharts(data);
    });
    this.aiService.insights(this.month, this.year).subscribe(insights => this.insights.set(insights));
  }

  private renderCharts(data: Dashboard): void {
    this.barChart?.destroy();
    this.doughnutChart?.destroy();

    this.barChart = new Chart(this.barChartRef.nativeElement, {
      type: 'bar',
      data: {
        labels: data.monthlyChart.map(p => `T${p.month}/${p.year}`),
        datasets: [
          { label: 'Thu nhập', data: data.monthlyChart.map(p => p.income), backgroundColor: '#2e7d32' },
          { label: 'Chi tiêu', data: data.monthlyChart.map(p => p.expense), backgroundColor: '#c62828' }
        ]
      },
      options: { responsive: true, scales: { y: { beginAtZero: true } } }
    });

    this.doughnutChart = new Chart(this.doughnutChartRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: data.topExpenseCategories.map(c => c.categoryName),
        datasets: [{
          data: data.topExpenseCategories.map(c => c.total),
          backgroundColor: data.topExpenseCategories.map(c => c.color)
        }]
      },
      options: { responsive: true }
    });
  }

  ngOnDestroy(): void {
    this.barChart?.destroy();
    this.doughnutChart?.destroy();
  }
}
