import { useState, useEffect, useMemo } from 'react'
import { Modal, Form, Input, InputNumber, Select, Space, Button, Alert, message, Spin } from 'antd'
import { SaveOutlined, SendOutlined } from '@ant-design/icons'
import { requestsApi } from '../api'
import type { PurchaseRequest, CreateRequestDto, UpdateRequestDto } from '../types'
import { Urgency, UrgencyLabels } from '../types'

const { TextArea } = Input

interface RequestFormModalProps {
  open: boolean
  editId?: string | null
  onClose: () => void
  onSuccess: () => void
}

export default function RequestFormModal({ open, editId, onClose, onSuccess }: RequestFormModalProps) {
  const [form] = Form.useForm()
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [request, setRequest] = useState<PurchaseRequest | null>(null)

  const isEdit = !!editId

  useEffect(() => {
    if (!open) {
      form.resetFields()
      setRequest(null)
      return
    }
    if (isEdit) {
      fetchRequest()
    }
  }, [open, editId])

  const fetchRequest = async () => {
    setLoading(true)
    try {
      const data = await requestsApi.getById(editId!)
      setRequest(data)
      form.setFieldsValue({
        itemName: data.itemName,
        quantity: data.quantity,
        unitPrice: data.unitPrice,
        reason: data.reason,
        urgency: data.urgencyValue,
      })
    } catch (error: any) {
      message.error(error.message || '获取申请详情失败')
      onClose()
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async (andSubmit: boolean = false) => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const dto: CreateRequestDto | UpdateRequestDto = {
        itemName: values.itemName,
        quantity: values.quantity,
        unitPrice: values.unitPrice,
        reason: values.reason,
        urgency: values.urgency,
      }

      let requestId: string

      if (isEdit) {
        await requestsApi.update(editId!, dto)
        requestId = editId!
        message.success('保存成功')
      } else {
        const result = await requestsApi.create(dto)
        requestId = result.id
        message.success('创建成功')
      }

      if (andSubmit) {
        await requestsApi.submit(requestId)
        message.success('提交成功，已进入审批流程')
      }

      onSuccess()
    } catch (error: any) {
      if (error.errorFields) return
      message.error(error.message || '操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  const quantity = Form.useWatch('quantity', form) || 0
  const unitPrice = Form.useWatch('unitPrice', form) || 0
  const totalAmount = useMemo(() => quantity * unitPrice, [quantity, unitPrice])

  const title = isEdit
    ? `编辑采购申请${request ? ` · ${request.requestNumber}` : ''}`
    : '新建采购申请'

  return (
    <Modal
      title={title}
      open={open}
      onCancel={onClose}
      width={640}
      destroyOnClose
      maskClosable={false}
      footer={
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span style={{ fontSize: 13, color: '#8c8c8c' }}>
            预估总额：
            <span style={{ color: '#ff4d4f', fontWeight: 600, fontSize: 16 }}>
              ¥{totalAmount.toLocaleString()}
            </span>
          </span>
          <Space>
            <Button onClick={onClose}>取消</Button>
            <Button
              icon={<SaveOutlined />}
              loading={submitting}
              onClick={() => handleSave(false)}
            >
              保存草稿
            </Button>
            <Button
              type="primary"
              icon={<SendOutlined />}
              loading={submitting}
              onClick={() => handleSave(true)}
            >
              保存并提交
            </Button>
          </Space>
        </div>
      }
    >
      {loading ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '60px 0' }}>
          <Spin size="large" />
        </div>
      ) : (
        <>
          <Form
            form={form}
            layout="vertical"
            initialValues={{ urgency: Urgency.Normal }}
            style={{ marginTop: 8 }}
          >
            <Form.Item
              name="itemName"
              label="物品名称"
              rules={[
                { required: true, message: '请输入物品名称' },
                { max: 200, message: '物品名称不能超过200字' },
              ]}
            >
              <Input placeholder="请输入采购物品名称" />
            </Form.Item>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16 }}>
              <Form.Item
                name="quantity"
                label="采购数量"
                rules={[
                  { required: true, message: '请输入数量' },
                  { type: 'number', min: 1, message: '数量至少为1' },
                ]}
              >
                <InputNumber placeholder="数量" min={1} style={{ width: '100%' }} />
              </Form.Item>

              <Form.Item
                name="unitPrice"
                label="预估单价（元）"
                rules={[
                  { required: true, message: '请输入单价' },
                  { type: 'number', min: 0.01, message: '单价必须大于0' },
                ]}
              >
                <InputNumber placeholder="单价" min={0.01} precision={2} style={{ width: '100%' }} />
              </Form.Item>

              <Form.Item label="总金额">
                <div style={{
                  height: 32,
                  lineHeight: '32px',
                  fontSize: 18,
                  fontWeight: 600,
                  color: '#ff4d4f',
                }}>
                  ¥{totalAmount.toLocaleString()}
                </div>
              </Form.Item>
            </div>

            <Form.Item
              name="urgency"
              label="紧急程度"
              rules={[{ required: true, message: '请选择紧急程度' }]}
            >
              <Select
                style={{ width: 200 }}
                options={Object.entries(UrgencyLabels).map(([value, label]) => ({
                  value: Number(value),
                  label,
                }))}
              />
            </Form.Item>

            <Form.Item
              name="reason"
              label="采购原因"
              rules={[{ max: 1000, message: '采购原因不能超过1000字' }]}
            >
              <TextArea
                placeholder="请说明采购原因（选填）"
                rows={3}
                showCount
                maxLength={1000}
              />
            </Form.Item>
          </Form>

          <Alert
            type="info"
            showIcon
            style={{ marginTop: 4 }}
            message="审批规则"
            description={
              <div style={{ fontSize: 13, lineHeight: '22px' }}>
                <div>金额 ≤ 5,000 元：仅需部门经理审批</div>
                <div>金额 5,001 ~ 20,000 元：部门经理 → 财务总监</div>
                <div>金额 {'>'} 20,000 元：部门经理 → 财务总监 → 总经理</div>
              </div>
            }
          />
        </>
      )}
    </Modal>
  )
}
