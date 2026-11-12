export type Role = "Admin" | "Seller" | "StockKeeper";

export type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type SessionUser = {
  id: string;
  name: string;
  email: string;
  role: Role;
};

export type User = SessionUser & {
  active: boolean;
  createdAt: string;
};

export type Product = {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  category: string | null;
  unit: string;
  salePrice: number;
  costPrice: number;
  stock: number;
  minStock: number;
  active: boolean;
  belowMinimum: boolean;
  createdAt: string;
  updatedAt: string | null;
};

export type MovementType = "In" | "Out";
export type MovementOrigin = "Manual" | "Sale" | "SaleCancellation";

export type Movement = {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  type: MovementType;
  origin: MovementOrigin;
  quantity: number;
  unitCost: number;
  balanceAfter: number;
  note: string | null;
  salesOrderId: string | null;
  userName: string;
  createdAt: string;
};

export type OrderStatus = "Open" | "Confirmed" | "Cancelled";

export type OrderSummary = {
  id: string;
  number: number;
  customerName: string;
  status: OrderStatus;
  total: number;
  itemCount: number;
  sellerName: string;
  createdAt: string;
  confirmedAt: string | null;
};

export type Order = {
  id: string;
  number: number;
  customerName: string;
  customerDocument: string | null;
  notes: string | null;
  status: OrderStatus;
  total: number;
  sellerId: string;
  sellerName: string;
  createdAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
  items: {
    productId: string;
    sku: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    total: number;
  }[];
};

export type TurnoverRow = {
  productId: string;
  sku: string;
  name: string;
  category: string | null;
  openingStock: number;
  closingStock: number;
  averageStock: number;
  quantitySold: number;
  costOfGoodsSold: number;
  turnover: number;
  coverageDays: number | null;
};

export type TurnoverReport = {
  from: string;
  to: string;
  days: number;
  totalCostOfGoodsSold: number;
  overallTurnover: number;
  rows: Paged<TurnoverRow>;
};

export type Dashboard = {
  activeProducts: number;
  belowMinimum: number;
  stockValue: number;
  openOrders: number;
  salesThisMonth: number;
  ordersThisMonth: number;
  last14Days: { date: string; total: number; orders: number }[];
  lowStock: { id: string; sku: string; name: string; stock: number; minStock: number }[];
};
