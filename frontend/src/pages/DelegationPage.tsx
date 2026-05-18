import { useState, useEffect } from 'react'
import {
  Table,
  Button,
  Modal,
  Form,
  DatePicker,
  Input,
  Select,
  Tag,
  Space,
  Tabs,
  message,
  Popconfirm,
  Card,
} from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  UserSwitchOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { Dayjs } from 'dayjs'
import { useStore } from '../store/useStore'
import { delegationsApi, usersApi } from '../api'
import type { Delegation, User, CreateDelegationDto, UpdateDelegationDto } from '../types'
import { UserRoleLabels } from '../types'

const { TextArea } = Input
const { RangePicker } = DatePicker
const { Option } = Select

interface DelegationFormData {
  trusteeId: string
  dateRange: [Dayjs, Dayjs]
  reason?: string
  isActive: boolean
}

export default function DelegationPage() {
  const { user, grantedDelegations, receivedDelegations, refreshDelegations } = useStore()
  const [users, setUsers] = useState<User[]>([])
  const [modalVisible, setModalVisible] = useState(false)
  const [editingDelegation, setEditingDelegation] = useState<Delegation | null>(null)
  const [form] = Form.useForm<DelegationFormData>()
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    await refreshDelegations()
    try {
      const allUsers = await usersApi.getAll()
      setUsers(allUsers)
    } catch (error) {
      console.error('获取用户列表失败', error)
    }
  }

  const handleCreate = () => {
    setEditingDelegation(null)
    form.resetFields()
    form.setFieldsValue({
      isActive: true,
    })
    setModalVisible(true)
  }

  const handleEdit = (record: Delegation) => {
    setEditingDelegation(record)
    form.setFieldsValue({
      trusteeId: record.trusteeId,
      dateRange: [dayjs(record.startDate), dayjs(record.endDate)],
      reason: record.reason,
      isActive: record.isActive,
    })
    setModalVisible(true)
  }

  const handleDelete = async (id: string) => {
    try {
      await delegationsApi.delete(id)
      message.success('删除成功')
      await refreshDelegations()
    } catch (error) {
      message.error('删除失败')
    }
  }

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const dto: CreateDelegationDto | UpdateDelegationDto = {
        trusteeId: values.trusteeId,
        startDate: values.dateRange[0].startOf('day').toISOString(),
        endDate: values.dateRange[1].endOf('day').toISOString(),
        reason: values.reason,
        ...(editingDelegation ? { isActive: values.isActive } : {}),
      }

      if (editingDelegation) {
        await delegationsApi.update(editingDelegation.id, dto as UpdateDelegationDto)
        message.success('更新成功')
      } else {
        await delegationsApi.create(dto as CreateDelegationDto)
        message.success('创建成功')
      }

      setModalVisible(false)
      await refreshDelegations()
    } catch (error) {
      message.error(editingDelegation ? '更新失败' : '创建失败')
    } finally {
      setSubmitting(false)
    }
  }

  const getStatusTag = (record: Delegation) => {
    const now = dayjs()
    const start = dayjs(record.startDate)
    const end = dayjs(record.endDate)

    if (!record.isActive) {
      return <Tag color="default">已停用</Tag>
    }
    if (now.isBefore(start)) {
      return <Tag color="blue">未开始</Tag>
    }
    if (now.isAfter(end)) {
      return <Tag color="gray">已过期</Tag>
    }
    return <Tag color="green">生效中</Tag>
  }

  const grantedColumns: ColumnsType<Delegation> = [
    {
      title: '被委托人',
      dataIndex: 'trusteeName',
      key: 'trusteeName',
      width: 120,
    },
    {
      title: '被委托人角色',
      dataIndex: 'trusteeRole',
      key: 'trusteeRole',
      width: 120,
      render: (role: string) => UserRoleLabels[role as keyof typeof UserRoleLabels] || role,
    },
    {
      title: '委托开始日期',
      dataIndex: 'startDate',
      key: 'startDate',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '委托结束日期',
      dataIndex: 'endDate',
      key: 'endDate',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '委托原因',
      dataIndex: 'reason',
      key: 'reason',
      ellipsis: true,
    },
    {
      title: '状态',
      key: 'status',
      width: 100,
      render: (_, record) => getStatusTag(record),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) => (
        <Space>
          <Button type="link" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
            编辑
          </Button>
          <Popconfirm title="确定删除该委托？" onConfirm={() => handleDelete(record.id)}>
            <Button type="link" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  const receivedColumns: ColumnsType<Delegation> = [
    {
      title: '委托人',
      dataIndex: 'grantorName',
      key: 'grantorName',
      width: 120,
    },
    {
      title: '委托人角色',
      dataIndex: 'grantorRole',
      key: 'grantorRole',
      width: 120,
      render: (role: string) => UserRoleLabels[role as keyof typeof UserRoleLabels] || role,
    },
    {
      title: '委托开始日期',
      dataIndex: 'startDate',
      key: 'startDate',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '委托结束日期',
      dataIndex: 'endDate',
      key: 'endDate',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '委托原因',
      dataIndex: 'reason',
      key: 'reason',
      ellipsis: true,
    },
    {
      title: '状态',
      key: 'status',
      width: 100,
      render: (_, record) => getStatusTag(record),
    },
  ]

  const tabItems = [
    {
      key: 'granted',
      label: '我发起的委托',
      children: (
        <Card
          extra={
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              新建委托
            </Button>
          }
        >
          <Table
            columns={grantedColumns}
            dataSource={grantedDelegations}
            rowKey="id"
            pagination={{ pageSize: 10 }}
          />
        </Card>
      ),
    },
    {
      key: 'received',
      label: '我收到的委托',
      children: (
        <Card>
          <Table
            columns={receivedColumns}
            dataSource={receivedDelegations}
            rowKey="id"
            pagination={{ pageSize: 10 }}
          />
        </Card>
      ),
    },
  ]

  const canCreateDelegation = user?.roleValue === 1 || user?.roleValue === 2 || user?.roleValue === 3

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 16 }}>
        <UserSwitchOutlined style={{ fontSize: 24, marginRight: 12, color: '#1677FF' }} />
        <h2 style={{ margin: 0 }}>审批委托管理</h2>
      </div>

      {canCreateDelegation ? (
        <Tabs defaultActiveKey="granted" items={tabItems} />
      ) : (
        <Card>
          <Table
            columns={receivedColumns}
            dataSource={receivedDelegations}
            rowKey="id"
            pagination={{ pageSize: 10 }}
          />
        </Card>
      )}

      <Modal
        title={editingDelegation ? '编辑委托' : '新建委托'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        confirmLoading={submitting}
        width={500}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="trusteeId"
            label="被委托人"
            rules={[{ required: true, message: '请选择被委托人' }]}
          >
            <Select placeholder="请选择被委托人" showSearch optionFilterProp="children">
              {users
                .filter((u) => u.id !== user?.id)
                .map((u) => (
                  <Option key={u.id} value={u.id}>
                    {u.displayName} ({UserRoleLabels[u.roleValue] || u.role})
                  </Option>
                ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="dateRange"
            label="委托有效期"
            rules={[{ required: true, message: '请选择委托有效期' }]}
          >
            <RangePicker style={{ width: '100%' }} minDate={dayjs()} />
          </Form.Item>

          <Form.Item name="reason" label="委托原因">
            <TextArea rows={3} placeholder="请输入委托原因（可选）" maxLength={500} showCount />
          </Form.Item>

          {editingDelegation && (
            <Form.Item name="isActive" label="是否启用" valuePropName="checked">
              <Select>
                <Option value={true}>启用</Option>
                <Option value={false}>停用</Option>
              </Select>
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  )
}
