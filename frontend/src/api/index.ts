import axios from 'axios'
import type {
  User,
  PurchaseRequest,
  Notification,
  DashboardData,
  CreateRequestDto,
  UpdateRequestDto,
  Delegation,
  CreateDelegationDto,
} from '../types'

interface LoginResult extends User {
  token: string
  tokenType: string
  expiresAt: string
}

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  const existingCorrelationId = localStorage.getItem('correlation_seed')
  if (!existingCorrelationId) {
    localStorage.setItem('correlation_seed', crypto.randomUUID())
  }

  config.headers['X-Correlation-ID'] = `${localStorage.getItem('correlation_seed')}-${Date.now()}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('user')
      localStorage.removeItem('access_token')
    }

    const message = error.response?.data?.message || '请求失败'
    return Promise.reject(new Error(message))
  },
)

export const authApi = {
  login: (username: string, password: string) =>
    api.post<LoginResult>('/auth/login', { username, password }).then((res) => res.data),
}

export const usersApi = {
  getAll: () => api.get<User[]>('/users').then((res) => res.data),
  getById: (id: string) => api.get<User>(`/users/${id}`).then((res) => res.data),
}

export const requestsApi = {
  getAll: (params?: { status?: number }) =>
    api.get<PurchaseRequest[]>('/requests', { params }).then((res) => res.data),
  getById: (id: string) => api.get<PurchaseRequest>(`/requests/${id}`).then((res) => res.data),
  create: (dto: CreateRequestDto) => api.post('/requests', dto).then((res) => res.data),
  update: (id: string, dto: UpdateRequestDto) => api.put(`/requests/${id}`, dto).then((res) => res.data),
  submit: (id: string) => api.post(`/requests/${id}/submit`).then((res) => res.data),
  cancel: (id: string) => api.post(`/requests/${id}/cancel`).then((res) => res.data),
}

export const approvalsApi = {
  getPending: () => api.get<PurchaseRequest[]>('/approvals/pending').then((res) => res.data),
  approve: (id: string, comment?: string) =>
    api.post(`/approvals/${id}/approve`, { comment }).then((res) => res.data),
  reject: (id: string, comment?: string) =>
    api.post(`/approvals/${id}/reject`, { comment }).then((res) => res.data),
  return: (id: string, comment?: string) =>
    api.post(`/approvals/${id}/return`, { comment }).then((res) => res.data),
  getHistory: (requestId: string) =>
    api.get(`/approvals/${requestId}/history`).then((res) => res.data),
}

export const notificationsApi = {
  getAll: (unreadOnly?: boolean) =>
    api.get<Notification[]>('/notifications', { params: { unreadOnly } }).then((res) => res.data),
  getUnreadCount: () =>
    api.get<{ count: number }>('/notifications/unread-count').then((res) => res.data.count),
  markAsRead: (id: string) => api.put(`/notifications/${id}/read`).then((res) => res.data),
  markAllAsRead: () => api.put('/notifications/read-all').then((res) => res.data),
}

export const statisticsApi = {
  getDashboard: () => api.get<DashboardData>('/statistics/dashboard').then((res) => res.data),
}

export const delegationsApi = {
  create: (dto: CreateDelegationDto) => api.post('/delegations', dto).then((res) => res.data),
  revoke: (id: string) => api.post(`/delegations/${id}/revoke`).then((res) => res.data),
  getAsDelegator: () => api.get<Delegation[]>('/delegations/as-delegator').then((res) => res.data),
  getAsDelegate: () => api.get<Delegation[]>('/delegations/as-delegate').then((res) => res.data),
  getActive: () => api.get<Delegation | null>('/delegations/active').then((res) => res.data),
  getAll: () => api.get<Delegation[]>('/delegations').then((res) => res.data),
  getAvailableDelegates: () => api.get<User[]>('/delegations/available-delegates').then((res) => res.data),
}

export default api
