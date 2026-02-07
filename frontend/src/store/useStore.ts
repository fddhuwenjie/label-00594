import { create } from 'zustand'
import type { User } from '../types'

interface AppState {
  user: User | null
  setUser: (user: User | null) => void
  logout: () => void
  unreadCount: number
  setUnreadCount: (count: number) => void
}

export const useStore = create<AppState>((set) => ({
  user: (() => {
    const userStr = localStorage.getItem('user')
    return userStr ? JSON.parse(userStr) : null
  })(),
  
  setUser: (user) => {
    if (user) {
      localStorage.setItem('user', JSON.stringify(user))
    } else {
      localStorage.removeItem('user')
    }
    set({ user })
  },
  
  logout: () => {
    localStorage.removeItem('user')
    set({ user: null, unreadCount: 0 })
  },
  
  unreadCount: 0,
  setUnreadCount: (count) => set({ unreadCount: count }),
}))
