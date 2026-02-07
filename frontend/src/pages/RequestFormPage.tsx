import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Card, Form, Input, InputNumber, Select, Button, Space, Typography, message, Spin } from 'antd'
import { SaveOutlined, SendOutlined, ArrowLeftOutlined } from '@ant-design/icons'
import { requestsApi } from '../api'
import type { PurchaseRequest, CreateRequestDto, UpdateRequestDto } from '../types'
import { Urgency, UrgencyLabels, RequestStatus } from '../types'

const { Title, Text } = Typography
const { TextArea } = Input

export default function RequestFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const [form] = Form.useForm()
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [request, setRequest] = useState<PurchaseRequest | null>(null)

  const isEdit = !!id

  useEffect(() => {
    if (isEdit) {
      fetchRequest()
    }
  }, [id])

  const fetchRequest = async () => {
    setLoading(true)
    try {
      const data = await requestsApi.getById(id!)
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
      navigate('/requests')
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
        await requestsApi.update(id!, dto)
        requestId = id!
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

      navigate('/requests')
    } catch (error: any) {
      if (error.errorFields) {
        // 表单验证错误
        return
      }
      message.error(error.message || '操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 计算总金额
  const quantity = Form.useWatch('quantity', form) || 0
  const unitPrice = Form.useWatch('unitPrice', form) || 0
  const totalAmount = quantity * unitPrice

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 400 }}>
        <Spin size="large" />
      </div>
    )
  }

  // 检查是否可编辑
  if (isEdit && request && 
      request.statusValue !== RequestStatus.Draft && 
      request.statusValue !== RequestStatus.Returned) {
    message.warning('当前状态不可编辑')
    navigate('/requests')
    return null
  }

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 24 }}>
        <Button 
          type="text" 
          icon={<ArrowLeftOutlined />} 
          onClick={() => navigate('/requests')}
          style={{ marginRight: 8 }}
        />
        <Title level={4} style={{ margin: 0 }}>
          {isEdit ? '编辑采购申请' : '新建采购申请'}
        </Title>
        {isEdit && request && (
          <Text type="secondary" style={{ marginLeft: 16 }}>
            申请编号：{request.requestNumber}
          </Text>
        )}
      </div>

      <Card bordered={false} style={{ borderRadius: 8, maxWidth: 800 }}>
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            urgency: Urgency.Normal,
          }}
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

          <Space style={{ display: 'flex' }} align="start">
            <Form.Item
              name="quantity"
              label="采购数量"
              rules={[
                { required: true, message: '请输入采购数量' },
                { type: 'number', min: 1, message: '数量至少为1' },
              ]}
              style={{ width: 200 }}
            >
              <InputNumber
                placeholder="请输入数量"
                min={1}
                style={{ width: '100%' }}
              />
            </Form.Item>

            <Form.Item
              name="unitPrice"
              label="预估单价（元）"
              rules={[
                { required: true, message: '请输入单价' },
                { type: 'number', min: 0.01, message: '单价必须大于0' },
              ]}
              style={{ width: 200 }}
            >
              <InputNumber
                placeholder="请输入单价"
                min={0.01}
                precision={2}
                style={{ width: '100%' }}
              />
            </Form.Item>

            <Form.Item label="总金额" style={{ width: 200 }}>
              <div style={{ 
                height: 32, 
                lineHeight: '32px', 
                fontSize: 18, 
                fontWeight: 600,
                color: '#FF4D4F' 
              }}>
                ¥{totalAmount.toLocaleString()}
              </div>
            </Form.Item>
          </Space>

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
              placeholder="请说明采购原因"
              rows={4}
              showCount
              maxLength={1000}
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 32 }}>
            <Space>
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
              <Button onClick={() => navigate('/requests')}>
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>

      {/* 审批规则提示 */}
      <Card 
        bordered={false} 
        style={{ borderRadius: 8, maxWidth: 800, marginTop: 16 }}
        size="small"
      >
        <div style={{ color: '#8C8C8C', fontSize: 13 }}>
          <div style={{ marginBottom: 8, color: '#595959', fontWeight: 500 }}>审批规则说明：</div>
          <div>• 金额 ≤ 5,000 元：仅需部门经理审批</div>
          <div>• 金额 5,001 ~ 20,000 元：部门经理 → 财务总监</div>
          <div>• 金额 {'>'} 20,000 元：部门经理 → 财务总监 → 总经理</div>
        </div>
      </Card>
    </div>
  )
}
