import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Budget, BudgetAlert, Category, ClassificationResult, Dashboard, ImportResult,
  PagedResult, Report, Transaction, TransactionType, Wallet, WalletType
} from './models';

function buildParams(obj: Record<string, unknown>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(obj)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private http = inject(HttpClient);

  list(search?: string, type?: TransactionType, page = 1, pageSize = 100): Observable<PagedResult<Category>> {
    return this.http.get<PagedResult<Category>>('/api/categories',
      { params: buildParams({ search, type, page, pageSize }) });
  }

  create(data: Partial<Category>): Observable<Category> {
    return this.http.post<Category>('/api/categories', data);
  }

  update(id: string, data: Partial<Category>): Observable<Category> {
    return this.http.put<Category>(`/api/categories/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/categories/${id}`);
  }
}

export interface TransactionFilter {
  fromDate?: string;
  toDate?: string;
  type?: TransactionType;
  categoryId?: string;
  walletId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private http = inject(HttpClient);

  list(filter: TransactionFilter): Observable<PagedResult<Transaction>> {
    return this.http.get<PagedResult<Transaction>>('/api/transactions',
      { params: buildParams({ ...filter }) });
  }

  create(data: object): Observable<Transaction> {
    return this.http.post<Transaction>('/api/transactions', data);
  }

  update(id: string, data: object): Observable<Transaction> {
    return this.http.put<Transaction>(`/api/transactions/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/transactions/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class WalletService {
  private http = inject(HttpClient);

  list(): Observable<Wallet[]> {
    return this.http.get<Wallet[]>('/api/wallets');
  }

  create(data: { name: string; type: WalletType; initialBalance: number; currency: string }): Observable<Wallet> {
    return this.http.post<Wallet>('/api/wallets', data);
  }

  update(id: string, data: object): Observable<Wallet> {
    return this.http.put<Wallet>(`/api/wallets/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/wallets/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private http = inject(HttpClient);

  list(month: number, year: number): Observable<Budget[]> {
    return this.http.get<Budget[]>('/api/budgets', { params: buildParams({ month, year }) });
  }

  alerts(month: number, year: number): Observable<BudgetAlert[]> {
    return this.http.get<BudgetAlert[]>('/api/budgets/alerts', { params: buildParams({ month, year }) });
  }

  create(data: { categoryId: string; amount: number; month: number; year: number }): Observable<Budget> {
    return this.http.post<Budget>('/api/budgets', data);
  }

  update(id: string, amount: number): Observable<Budget> {
    return this.http.put<Budget>(`/api/budgets/${id}`, { amount });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/budgets/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  get(month: number, year: number): Observable<Dashboard> {
    return this.http.get<Dashboard>('/api/dashboard', { params: buildParams({ month, year }) });
  }
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private http = inject(HttpClient);

  get(fromDate: string, toDate: string): Observable<Report> {
    return this.http.get<Report>('/api/reports', { params: buildParams({ fromDate, toDate }) });
  }

  exportFile(format: 'excel' | 'pdf', fromDate: string, toDate: string): Observable<Blob> {
    return this.http.get(`/api/reports/export/${format}`, {
      params: buildParams({ fromDate, toDate }),
      responseType: 'blob'
    });
  }
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private http = inject(HttpClient);

  classify(description: string): Observable<ClassificationResult> {
    return this.http.post<ClassificationResult>('/api/ai/classify', { description });
  }

  insights(month: number, year: number): Observable<string[]> {
    return this.http.get<string[]>('/api/ai/insights', { params: buildParams({ month, year }) });
  }
}

@Injectable({ providedIn: 'root' })
export class ImportService {
  private http = inject(HttpClient);

  importBankStatement(file: File, walletId: string): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file);
    form.append('walletId', walletId);
    return this.http.post<ImportResult>('/api/import/bank-statement', form);
  }
}
