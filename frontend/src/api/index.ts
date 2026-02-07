import axios from 'axios'
import type { 
  User, 
  PurchaseRequest, 
  Notification, 
  DashboardData,
  CreateRequestDto,
  UpdateRequestDto
} from '../types'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

// 请求拦截器：添加用户ID
api.interceptors.request.use((config) => {
  const userStr = localStorage.getItem('user')
  if (userStr) {
    const user = JSON.parse(userStr)
    config.headers['X-User-Id'] = user.id
  }
  return config
})

// 响应拦截器
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const message = error.response?.data?.message || '请求失败'
    return Promise.reject(new Error(message))
  }
)

// Auth API
export const authApi = {
  login: (username: string) => 
    api.post<User>('/auth/login', { username }).then(res => res.data),
}

// Users API
export const usersApi = {
  getAll: () => 
    api.get<User[]>('/users').then(res => res.data),
  getById: (id: string) => 
    api.get<User>(`/users/${id}`).then(res => res.data),
}

// Requests API
export const requestsApi = {
  getAll: (params?: { userId?: string; status?: number }) => 
    api.get<PurchaseRequest[]>('/requests', { params }).then(res => res.data),
  getById: (id: string) => 
    api.get<PurchaseRequest>(`/requests/${id}`).then(res => res.data),
  create: (dto: CreateRequestDto) => 
    api.post('/requests', dto).then(res => res.data),
  update: (id: string, dto: UpdateRequestDto) => 
    api.put(`/requests/${id}`, dto).then(res => res.data),
  submit: (id: string) => 
    api.post(`/requests/${id}/submit`).then(res => res.data),
  cancel: (id: string) => 
    api.post(`/requests/${id}/cancel`).then(res => res.data),
}

// Approvals API
export const approvalsApi = {
  getPending: () => 
    api.get<PurchaseRequest[]>('/approvals/pending').then(res => res.data),
  approve: (id: string, comment?: string) => 
    api.post(`/approvals/${id}/approve`, { comment }).then(res => res.data),
  reject: (id: string, comment?: string) => 
    api.post(`/approvals/${id}/reject`, { comment }).then(res => res.data),
  return: (id: string, comment?: string) => 
    api.post(`/approvals/${id}/return`, { comment }).then(res => res.data),
  getHistory: (requestId: string) => 
    api.get(`/approvals/${requestId}/history`).then(res => res.data),
}

// Notifications API
export const notificationsApi = {
  getAll: (unreadOnly?: boolean) => 
    api.get<Notification[]>('/notifications', { params: { unreadOnly } }).then(res => res.data),
  getUnreadCount: () => 
    api.get<{ count: number }>('/notifications/unread-count').then(res => res.data.count),
  markAsRead: (id: string) => 
    api.put(`/notifications/${id}/read`).then(res => res.data),
  markAllAsRead: () => 
    api.put('/notifications/read-all').then(res => res.data),
}

// Statistics API
export const statisticsApi = {
  getDashboard: () => 
    api.get<DashboardData>('/statistics/dashboard').then(res => res.data),
}

export default api
