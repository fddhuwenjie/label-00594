import { create } from 'zustand'
import type { Delegation, ApproverOption } from '../types'
import { delegationsApi } from '../api'

interface DelegationState {
  delegationsAsDelegator: Delegation[]
  delegationsAsDelegatee: Delegation[]
  activeDelegation: Delegation | null
  approvers: ApproverOption[]
  loading: boolean
  fetchMyDelegations: () => Promise<void>
  fetchActiveDelegation: () => Promise<void>
  fetchApprovers: () => Promise<void>
  createDelegation: (data: { delegateeId: string; startDate: string; endDate: string; reason?: string }) => Promise<boolean>
  revokeDelegation: (id: string) => Promise<boolean>
}

export const useDelegationStore = create<DelegationState>((set, get) => ({
  delegationsAsDelegator: [],
  delegationsAsDelegatee: [],
  activeDelegation: null,
  approvers: [],
  loading: false,

  fetchMyDelegations: async () => {
    set({ loading: true })
    try {
      const data = await delegationsApi.getMy()
      set({
        delegationsAsDelegator: data.asDelegator,
        delegationsAsDelegatee: data.asDelegatee,
        loading: false,
      })
    } catch {
      set({ loading: false })
    }
  },

  fetchActiveDelegation: async () => {
    try {
      const data = await delegationsApi.getActive()
      set({ activeDelegation: data })
    } catch {
      set({ activeDelegation: null })
    }
  },

  fetchApprovers: async () => {
    try {
      const data = await delegationsApi.getApprovers()
      set({ approvers: data })
    } catch {
      set({ approvers: [] })
    }
  },

  createDelegation: async (data) => {
    try {
      await delegationsApi.create(data)
      await get().fetchMyDelegations()
      await get().fetchActiveDelegation()
      return true
    } catch {
      return false
    }
  },

  revokeDelegation: async (id) => {
    try {
      await delegationsApi.revoke(id)
      await get().fetchMyDelegations()
      await get().fetchActiveDelegation()
      return true
    } catch {
      return false
    }
  },
}))
