/**
 * KromicStore API Client
 * TypeScript implementation for integrating with KromicStore API
 * 
 * Usage:
 * const client = new KromicStoreClient('https://api.kromic-store.com/api/v1');
 * const products = await client.products.list();
 */

export interface ApiResponse<T> {
  data: T;
  meta?: {
    requestId: string;
    timestamp: string;
    tenantId?: string;
  };
}

export interface PagedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

export interface ErrorResponse {
  error: {
    code: string;
    message: string;
    details?: Array<{
      field?: string;
      code: string;
      message: string;
    }>;
    traceId?: string;
  };
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface Product {
  id: string;
  tenantId: string;
  name: string;
  sku: string;
  description: string;
  price: number;
  categoryId: string;
  stock: number;
  status: 'Draft' | 'Published' | 'Archived';
  images: Array<{
    id: string;
    url: string;
    alt: string;
    displayOrder: number;
  }>;
  createdAt: string;
  updatedAt: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  status: 'Pending' | 'Confirmed' | 'Paid' | 'Shipped' | 'Delivered' | 'Cancelled';
  total: number;
  subtotal: number;
  tax: number;
  shipping: number;
  items: OrderItem[];
  shippingAddress: Address;
  billingAddress: Address;
  payment?: {
    id: string;
    status: string;
    paidAt?: string;
  };
  createdAt: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  productSku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface Customer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  orderCount: number;
  lifetimeValue: number;
  lastOrderAt?: string;
  createdAt: string;
}

export interface Webhook {
  id: string;
  url: string;
  events: string[];
  secret: string;
  isActive: boolean;
  createdAt: string;
}

export class KromicStoreClient {
  private baseUrl: string;
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private tokenExpiry: number | null = null;

  // Sub-clients for different resources
  public auth: AuthClient;
  public products: ProductsClient;
  public orders: OrdersClient;
  public customers: CustomersClient;
  public payments: PaymentsClient;
  public webhooks: WebhooksClient;
  public config: ConfigClient;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
    this.loadTokens();

    this.auth = new AuthClient(this);
    this.products = new ProductsClient(this);
    this.orders = new OrdersClient(this);
    this.customers = new CustomersClient(this);
    this.payments = new PaymentsClient(this);
    this.webhooks = new WebhooksClient(this);
    this.config = new ConfigClient(this);
  }

  /**
   * Set authentication token
   */
  setToken(accessToken: string, refreshToken: string, expiresIn: number) {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    this.tokenExpiry = Date.now() + expiresIn * 1000;

    // Store in localStorage
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('tokenExpiry', this.tokenExpiry.toString());
  }

  /**
   * Load tokens from localStorage
   */
  private loadTokens() {
    this.accessToken = localStorage.getItem('accessToken');
    this.refreshToken = localStorage.getItem('refreshToken');
    const expiry = localStorage.getItem('tokenExpiry');
    this.tokenExpiry = expiry ? parseInt(expiry) : null;
  }

  /**
   * Get current access token
   */
  getAccessToken(): string | null {
    return this.accessToken;
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(): boolean {
    if (!this.tokenExpiry) return true;
    return Date.now() > this.tokenExpiry - 60000; // 1 minute buffer
  }

  /**
   * Clear tokens
   */
  clearTokens() {
    this.accessToken = null;
    this.refreshToken = null;
    this.tokenExpiry = null;

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tokenExpiry');
  }

  /**
   * Make HTTP request
   */
  async request<T>(
    method: string,
    endpoint: string,
    data?: any,
    options: { retry?: boolean } = {}
  ): Promise<T> {
    // Ensure token is fresh
    if (this.isTokenExpired() && this.refreshToken) {
      await this.auth.refreshToken();
    }

    const url = `${this.baseUrl}${endpoint}`;
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (this.accessToken) {
      headers['Authorization'] = `Bearer ${this.accessToken}`;
    }

    try {
      const response = await fetch(url, {
        method,
        headers,
        body: data ? JSON.stringify(data) : undefined,
      });

      if (response.status === 401 && this.refreshToken && options.retry !== false) {
        // Try to refresh token and retry
        await this.auth.refreshToken();
        return this.request<T>(method, endpoint, data, { retry: false });
      }

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        throw new ApiError(
          errorData.error.message,
          response.status,
          errorData.error.code,
          errorData.error.details
        );
      }

      return response.json();
    } catch (error) {
      if (error instanceof ApiError) throw error;
      throw new ApiError(
        error instanceof Error ? error.message : 'Unknown error',
        0,
        'NETWORK_ERROR'
      );
    }
  }
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public code: string,
    public details?: Array<{ field?: string; code: string; message: string }>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Authentication Client
 */
class AuthClient {
  constructor(private client: KromicStoreClient) {}

  async register(companyName: string, email: string, password: string, country: string) {
    const response = await this.client.request<ApiResponse<any>>(
      'POST',
      '/auth/register',
      { companyName, email, password, country }
    );
    return response.data;
  }

  async login(email: string, password: string) {
    const response = await this.client.request<ApiResponse<AuthResponse & { user: any }>>(
      'POST',
      '/auth/login',
      { email, password }
    );
    const { accessToken, refreshToken, expiresIn } = response.data;
    this.client.setToken(accessToken, refreshToken, expiresIn);
    return response.data;
  }

  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) throw new Error('No refresh token available');

    const response = await this.client.request<ApiResponse<AuthResponse>>(
      'POST',
      '/auth/refresh',
      { refreshToken }
    );
    const { accessToken, expiresIn } = response.data;
    this.client.setToken(accessToken, refreshToken, expiresIn);
    return response.data;
  }

  async logout() {
    try {
      await this.client.request('POST', '/auth/logout');
    } finally {
      this.client.clearTokens();
    }
  }

  async oauthGoogle(code: string, redirectUri: string) {
    const response = await this.client.request<ApiResponse<AuthResponse & { user: any; isNewAccount: boolean }>>(
      'POST',
      '/auth/oauth/google',
      { code, redirectUri }
    );
    const { accessToken, refreshToken, expiresIn } = response.data;
    this.client.setToken(accessToken, refreshToken, expiresIn);
    return response.data;
  }
}

/**
 * Products Client
 */
class ProductsClient {
  constructor(private client: KromicStoreClient) {}

  async list(page = 1, pageSize = 20, filters: any = {}) {
    const queryParams = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
      ...Object.entries(filters).reduce((acc, [key, value]) => {
        if (value) acc[key] = String(value);
        return acc;
      }, {} as any),
    });

    const response = await this.client.request<PagedResponse<Product>>(
      'GET',
      `/products?${queryParams}`
    );
    return response;
  }

  async get(id: string) {
    const response = await this.client.request<ApiResponse<Product>>(
      'GET',
      `/products/${id}`
    );
    return response.data;
  }

  async create(data: Partial<Product>) {
    const response = await this.client.request<ApiResponse<Product>>(
      'POST',
      '/products',
      data
    );
    return response.data;
  }

  async update(id: string, data: Partial<Product>) {
    const response = await this.client.request<ApiResponse<Product>>(
      'PUT',
      `/products/${id}`,
      data
    );
    return response.data;
  }

  async publish(id: string) {
    const response = await this.client.request<ApiResponse<Product>>(
      'POST',
      `/products/${id}/publish`
    );
    return response.data;
  }

  async unpublish(id: string) {
    const response = await this.client.request<ApiResponse<Product>>(
      'POST',
      `/products/${id}/unpublish`
    );
    return response.data;
  }

  async delete(id: string) {
    await this.client.request('DELETE', `/products/${id}`);
  }
}

/**
 * Orders Client
 */
class OrdersClient {
  constructor(private client: KromicStoreClient) {}

  async list(page = 1, pageSize = 20, filters: any = {}) {
    const queryParams = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
      ...Object.entries(filters).reduce((acc, [key, value]) => {
        if (value) acc[key] = String(value);
        return acc;
      }, {} as any),
    });

    const response = await this.client.request<PagedResponse<Order>>(
      'GET',
      `/orders?${queryParams}`
    );
    return response;
  }

  async get(id: string) {
    const response = await this.client.request<ApiResponse<Order>>(
      'GET',
      `/orders/${id}`
    );
    return response.data;
  }

  async create(data: {
    customerId: string;
    items: { productId: string; quantity: number }[];
    shippingAddress: Address;
    billingAddress: Address;
  }) {
    const response = await this.client.request<ApiResponse<Order>>(
      'POST',
      '/orders',
      data
    );
    return response.data;
  }

  async confirm(id: string) {
    const response = await this.client.request<ApiResponse<Order>>(
      'POST',
      `/orders/${id}/confirm`
    );
    return response.data;
  }

  async ship(id: string, data?: { trackingNumber?: string; carrier?: string }) {
    const response = await this.client.request<ApiResponse<Order>>(
      'POST',
      `/orders/${id}/ship`,
      data
    );
    return response.data;
  }

  async deliver(id: string) {
    const response = await this.client.request<ApiResponse<Order>>(
      'POST',
      `/orders/${id}/deliver`
    );
    return response.data;
  }

  async cancel(id: string, reason?: string) {
    const response = await this.client.request<ApiResponse<Order>>(
      'POST',
      `/orders/${id}/cancel`,
      { reason }
    );
    return response.data;
  }
}

/**
 * Customers Client
 */
class CustomersClient {
  constructor(private client: KromicStoreClient) {}

  async list(page = 1, pageSize = 20) {
    const queryParams = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    const response = await this.client.request<PagedResponse<Customer>>(
      'GET',
      `/customers?${queryParams}`
    );
    return response;
  }

  async get(id: string) {
    const response = await this.client.request<ApiResponse<Customer>>(
      'GET',
      `/customers/${id}`
    );
    return response.data;
  }

  async create(data: Partial<Customer>) {
    const response = await this.client.request<ApiResponse<Customer>>(
      'POST',
      '/customers',
      data
    );
    return response.data;
  }

  async update(id: string, data: Partial<Customer>) {
    const response = await this.client.request<ApiResponse<Customer>>(
      'PUT',
      `/customers/${id}`,
      data
    );
    return response.data;
  }
}

/**
 * Payments Client
 */
class PaymentsClient {
  constructor(private client: KromicStoreClient) {}

  async create(orderId: string, amount?: number, currency = 'USD') {
    const response = await this.client.request<ApiResponse<any>>(
      'POST',
      '/payments/create',
      { orderId, amount, currency }
    );
    return response.data;
  }

  async verify(orderId: string, razorpayPaymentId: string, razorpayOrderId: string, razorpaySignature: string) {
    const response = await this.client.request<ApiResponse<any>>(
      'POST',
      '/payments/verify',
      {
        orderId,
        razorpayPaymentId,
        razorpayOrderId,
        razorpaySignature,
      }
    );
    return response.data;
  }
}

/**
 * Webhooks Client
 */
class WebhooksClient {
  constructor(private client: KromicStoreClient) {}

  async list() {
    const response = await this.client.request<PagedResponse<Webhook>>(
      'GET',
      '/webhooks'
    );
    return response;
  }

  async create(url: string, events: string[], description?: string) {
    const response = await this.client.request<ApiResponse<Webhook>>(
      'POST',
      '/webhooks',
      { url, events, description }
    );
    return response.data;
  }

  async test(id: string) {
    const response = await this.client.request<ApiResponse<any>>(
      'POST',
      `/webhooks/${id}/test`
    );
    return response.data;
  }

  async delete(id: string) {
    await this.client.request('DELETE', `/webhooks/${id}`);
  }
}

/**
 * Configuration Client
 */
class ConfigClient {
  constructor(private client: KromicStoreClient) {}

  async get(key: string) {
    const response = await this.client.request<ApiResponse<any>>(
      'GET',
      `/config/${key}`
    );
    return response.data;
  }

  async set(key: string, value: any) {
    const response = await this.client.request<ApiResponse<any>>(
      'PUT',
      `/config/${key}`,
      { value }
    );
    return response.data;
  }
}

/**
 * Usage Examples
 */

// Example 1: Authentication
async function exampleAuth() {
  const client = new KromicStoreClient('https://api.kromic-store.com/api/v1');

  // Register
  await client.auth.register('My Store', 'admin@mystore.com', 'SecurePass123!', 'US');

  // Login
  await client.auth.login('admin@mystore.com', 'SecurePass123!');

  // Now client can make authenticated requests
}

// Example 2: Product Management
async function exampleProducts() {
  const client = new KromicStoreClient('https://api.kromic-store.com/api/v1');
  await client.auth.login('admin@mystore.com', 'SecurePass123!');

  // Create product
  const product = await client.products.create({
    name: 'Awesome Product',
    sku: 'PROD-001',
    price: 99.99,
    stock: 100,
    categoryId: 'category-123',
  });

  // Publish product
  await client.products.publish(product.id);

  // List products
  const products = await client.products.list(1, 20, { status: 'Published' });
  console.log(products.data);
}

// Example 3: Order Management
async function exampleOrders() {
  const client = new KromicStoreClient('https://api.kromic-store.com/api/v1');
  await client.auth.login('admin@mystore.com', 'SecurePass123!');

  // Create order
  const order = await client.orders.create({
    customerId: 'customer-123',
    items: [{ productId: 'product-123', quantity: 2 }],
    shippingAddress: {
      street: '123 Main St',
      city: 'New York',
      state: 'NY',
      postalCode: '10001',
      country: 'US',
    },
    billingAddress: {
      street: '123 Main St',
      city: 'New York',
      state: 'NY',
      postalCode: '10001',
      country: 'US',
    },
  });

  // Confirm order
  await client.orders.confirm(order.id);

  // Process payment
  const payment = await client.payments.create(order.id, order.total);
  console.log('Payment initiated:', payment);
}

// Example 4: Webhook Management
async function exampleWebhooks() {
  const client = new KromicStoreClient('https://api.kromic-store.com/api/v1');
  await client.auth.login('admin@mystore.com', 'SecurePass123!');

  // Create webhook
  const webhook = await client.webhooks.create(
    'https://mystore.com/webhooks/kromic',
    ['order.created', 'order.updated', 'payment.received']
  );

  console.log('Webhook created:', webhook.id);
  console.log('Secret (store securely):', webhook.secret);

  // Test webhook
  await client.webhooks.test(webhook.id);
}

export default KromicStoreClient;
