export enum TransactionType {
  Income = 1,
  Expense = 2
}

export enum WalletType {
  Cash = 1,
  Bank = 2,
  Momo = 3,
  ZaloPay = 4,
  CreditCard = 5,
  Other = 99
}

export const WALLET_TYPE_LABELS: Record<number, string> = {
  [WalletType.Cash]: 'Tiền mặt',
  [WalletType.Bank]: 'Ngân hàng',
  [WalletType.Momo]: 'Momo',
  [WalletType.ZaloPay]: 'ZaloPay',
  [WalletType.CreditCard]: 'Thẻ tín dụng',
  [WalletType.Other]: 'Khác'
};

export interface User {
  id: string;
  email: string;
  fullName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface Category {
  id: string;
  name: string;
  type: TransactionType;
  icon: string;
  color: string;
}

export interface Wallet {
  id: string;
  name: string;
  type: WalletType;
  initialBalance: number;
  currency: string;
  currentBalance: number;
}

export interface Transaction {
  id: string;
  amount: number;
  type: TransactionType;
  note?: string;
  transactionDate: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  walletId: string;
  walletName: string;
}

export interface Budget {
  id: string;
  amount: number;
  month: number;
  year: number;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  spent: number;
  percentage: number;
}

export interface BudgetAlert {
  budgetId: string;
  categoryName: string;
  amount: number;
  spent: number;
  percentage: number;
  level: 'Warning' | 'Exceeded';
  message: string;
}

export interface MonthlyChartPoint {
  month: number;
  year: number;
  income: number;
  expense: number;
}

export interface CategorySummary {
  categoryId: string;
  categoryName: string;
  color: string;
  total: number;
}

export interface Dashboard {
  totalIncome: number;
  totalExpense: number;
  balance: number;
  monthlyChart: MonthlyChartPoint[];
  topExpenseCategories: CategorySummary[];
  wallets: Wallet[];
  budgetAlerts: BudgetAlert[];
}

export interface Report {
  fromDate: string;
  toDate: string;
  totalIncome: number;
  totalExpense: number;
  balance: number;
  incomeByCategory: CategorySummary[];
  expenseByCategory: CategorySummary[];
  transactions: Transaction[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ClassificationResult {
  categoryId?: string;
  categoryName?: string;
  confidence: number;
}

export interface ImportResult {
  imported: number;
  skipped: number;
  errors: string[];
}
