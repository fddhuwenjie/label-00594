import { create } from 'zustand'
import type { User, Delegation, ActiveGrantor } from '../types'
import { delegationsApi } from '../api'

interface AppState {
  user: User | null
  token: string | null
  setSession: (session: { user: User; token: string } | null) => void
  logout: () => void
  unreadCount: number
  setUnreadCount: (count: number) => void
  grantedDelegations: Delegation[]
  receivedDelegations: Delegation[]
  activeGrantors: ActiveGrantor[]
  delegationsLoading: boolean
  fetchGrantedDelegations: () => Promise<void>
  fetchReceivedDelegations: () => Promise<void>
  fetchActiveGrantors: () => Promise<void>
  refreshDelegations: () => Promise<void>
}

const loadUser = (): User | null => {
  const userStr = localStorage.getItem('user')
  return userStr ? JSON.parse(userStr) : null
}

const loadToken = (): string | null => {
  return localStorage.getItem('access_token')
}

export const useStore = create<AppState>((set) => ({
  user: loadUser(),
  token: loadToken(),

  setSession: (session) => {
    if (session) {
      localStorage.setItem('user', JSON.stringify(session.user))
      localStorage.setItem('access_token', session.token)
      set({ user: session.user, token: session.token })
      return
    }

    localStorage.removeItem('user')
    localStorage.removeItem('access_token')
    set({ user: null, token: null, unreadCount: 0, grantedDelegations: [], receivedDelegations: [], activeGrantors: [] })
  },

  logout: () => {
    localStorage.removeItem('user')
    localStorage.removeItem('access_token')
    set({ user: null, token: null, unreadCount: 0, grantedDelegations: [], receivedDelegations: [], activeGrantors: [] })
  },

  unreadCount: 0,
  setUnreadCount: (count) => set({ unreadCount: count }),

  grantedDelegations: [],
  receivedDelegations: [],
  activeGrantors: [],
  delegationsLoading: false,

  fetchGrantedDelegations: async () => {
    try {
      set({ delegationsLoading: true })
      const data = await delegationsApi.getGranted()
      set({ grantedDelegations: data })
    } catch (error) {
      console.error('获取发起的委托失败', error)
    } finally {
      set({ delegationsLoading: false })
    }
  },

  fetchReceivedDelegations: async () => {
    try {
      set({ delegationsLoading: true })
      const data = await delegationsApi.getReceived()
      set({ receivedDelegations: data })
    } catch (error) {
      console.error('获取收到的委托失败', error)
    } finally {
      set({ delegationsLoading: false })
    }
  },

  fetchActiveGrantors: async () => {
    try {
      const data = await delegationsApi.getActiveGrantors()
      set({ activeGrantors: data })
    } catch (error) {
      console.error('获取活跃委托人失败', error)
    }
  },

  refreshDelegations: async () => {
    await Promise.all([
      useStore.getState().fetchGrantedDelegations(),
      useStore.getState().fetchReceivedDelegations(),
      useStore.getState().fetchActiveGrantors(),
    ])
  },
}))
