import { create } from 'zustand'
import type { User } from '../types'

interface AppState {
  user: User | null
  token: string | null
  setSession: (session: { user: User; token: string } | null) => void
  logout: () => void
  unreadCount: number
  setUnreadCount: (count: number) => void
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
    set({ user: null, token: null, unreadCount: 0 })
  },

  logout: () => {
    localStorage.removeItem('user')
    localStorage.removeItem('access_token')
    set({ user: null, token: null, unreadCount: 0 })
  },

  unreadCount: 0,
  setUnreadCount: (count) => set({ unreadCount: count }),
}))
