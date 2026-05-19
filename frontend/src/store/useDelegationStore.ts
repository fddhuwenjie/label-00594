import { create } from 'zustand'
import type { Delegation } from '../types'
import { delegationsApi } from '../api'

interface DelegationState {
  myDelegationsAsDelegator: Delegation[]
  myDelegationsAsDelegate: Delegation[]
  activeDelegation: Delegation | null
  allDelegations: Delegation[]
  loading: boolean
  error: string | null
  fetchMyDelegationsAsDelegator: () => Promise<void>
  fetchMyDelegationsAsDelegate: () => Promise<void>
  fetchActiveDelegation: () => Promise<void>
  fetchAllDelegations: () => Promise<void>
  createDelegation: (dto: { delegateId: string; startDate: string; endDate: string; remark?: string }) => Promise<Delegation | null>
  revokeDelegation: (id: string) => Promise<boolean>
  clearError: () => void
}

export const useDelegationStore = create<DelegationState>((set) => ({
  myDelegationsAsDelegator: [],
  myDelegationsAsDelegate: [],
  activeDelegation: null,
  allDelegations: [],
  loading: false,
  error: null,

  fetchMyDelegationsAsDelegator: async () => {
    set({ loading: true, error: null })
    try {
      const data = await delegationsApi.getAsDelegator()
      set({ myDelegationsAsDelegator: data })
    } catch (err) {
      set({ error: err instanceof Error ? err.message : '获取委托列表失败' })
    } finally {
      set({ loading: false })
    }
  },

  fetchMyDelegationsAsDelegate: async () => {
    set({ loading: true, error: null })
    try {
      const data = await delegationsApi.getAsDelegate()
      set({ myDelegationsAsDelegate: data })
    } catch (err) {
      set({ error: err instanceof Error ? err.message : '获取被委托列表失败' })
    } finally {
      set({ loading: false })
    }
  },

  fetchActiveDelegation: async () => {
    set({ loading: true, error: null })
    try {
      const data = await delegationsApi.getActive()
      set({ activeDelegation: data })
    } catch (err) {
      set({ error: err instanceof Error ? err.message : '获取生效委托失败' })
    } finally {
      set({ loading: false })
    }
  },

  fetchAllDelegations: async () => {
    set({ loading: true, error: null })
    try {
      const data = await delegationsApi.getAll()
      set({ allDelegations: data })
    } catch (err) {
      set({ error: err instanceof Error ? err.message : '获取所有委托失败' })
    } finally {
      set({ loading: false })
    }
  },

  createDelegation: async (dto) => {
    set({ loading: true, error: null })
    try {
      const data = await delegationsApi.create(dto)
      await delegationsApi.getAsDelegator().then((list) => {
        set({ myDelegationsAsDelegator: list })
      })
      return data
    } catch (err) {
      const message = err instanceof Error ? err.message : '创建委托失败'
      set({ error: message })
      return null
    } finally {
      set({ loading: false })
    }
  },

  revokeDelegation: async (id: string) => {
    set({ loading: true, error: null })
    try {
      await delegationsApi.revoke(id)
      await delegationsApi.getAsDelegator().then((list) => {
        set({ myDelegationsAsDelegator: list })
      })
      await delegationsApi.getActive().then((active) => {
        set({ activeDelegation: active })
      })
      return true
    } catch (err) {
      set({ error: err instanceof Error ? err.message : '撤销委托失败' })
      return false
    } finally {
      set({ loading: false })
    }
  },

  clearError: () => set({ error: null }),
}))
