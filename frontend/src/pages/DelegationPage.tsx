import { useEffect, useState } from 'react'
import {
  Card,
  Table,
  Button,
  Modal,
  Form,
  Select,
  DatePicker,
  Input,
  Tag,
  Space,
  Tabs,
  message,
  Popconfirm,
  Alert,
} from 'antd'
import {
  PlusOutlined,
  DeleteOutlined,
  UserSwitchOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import { useDelegationStore } from '../store/useDelegationStore'
import { delegationsApi } from '../api'
import type { Delegation } from '../types'

const { RangePicker } = DatePicker
const { TextArea } = Input

export default function DelegationPage() {
  const {
    myDelegationsAsDelegator,
    myDelegationsAsDelegate,
    activeDelegation,
    loading,
    error,
    fetchMyDelegationsAsDelegator,
    fetchMyDelegationsAsDelegate,
    fetchActiveDelegation,
    createDelegation,
    revokeDelegation,
    clearError,
  } = useDelegationStore()

  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [form] = Form.useForm()
  const [delegateOptions, setDelegateOptions] = useState<{ id: string; displayName: string; role: string; department: string }[]>([])

  useEffect(() => {
    fetchMyDelegationsAsDelegator()
    fetchMyDelegationsAsDelegate()
    fetchActiveDelegation()
  }, [])

  useEffect(() => {
    if (error) {
      message.error(error)
      clearError()
    }
  }, [error])

  const handleOpenCreateModal = async () => {
    try {
      const users = await delegationsApi.getAvailableDelegates()
      setDelegateOptions(users)
      setCreateModalOpen(true)
      form.resetFields()
    } catch {
      message.error('获取可选用户列表失败')
    }
  }

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      const [startDate, endDate] = values.dateRange
      const result = await createDelegation({
        delegateId: values.delegateId,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
        remark: values.remark,
      })
      if (result) {
        message.success('委托创建成功')
        setCreateModalOpen(false)
        fetchActiveDelegation()
      }
    } catch (err) {
      if (!(err instanceof Error)) {
        return
      }
    }
  }

  const handleRevoke = async (id: string) => {
    const success = await revokeDelegation(id)
    if (success) {
      message.success('委托已撤销')
    }
  }

  const isActive = (d: Delegation) => {
    const now = new Date()
    const start = new Date(d.startDate)
    const end = new Date(d.endDate)
    return !d.isRevoked && start <= now && end >= now
  }

  const delegatorColumns = [
    {
      title: '被委托人',
      dataIndex: 'delegateName',
      key: 'delegateName',
    },
    {
      title: '委托开始',
      dataIndex: 'startDate',
      key: 'startDate',
      render: (v: string) => new Date(v).toLocaleString('zh-CN'),
    },
    {
      title: '委托结束',
      dataIndex: 'endDate',
      key: 'endDate',
      render: (v: string) => new Date(v).toLocaleString('zh-CN'),
    },
    {
      title: '状态',
      key: 'status',
      render: (_: unknown, record: Delegation) => {
        if (record.isRevoked) {
          return <Tag color="default">已撤销</Tag>
        }
        if (isActive(record)) {
          return <Tag color="success">生效中</Tag>
        }
        if (new Date(record.startDate) > new Date()) {
          return <Tag color="blue">待生效</Tag>
        }
        return <Tag color="default">已过期</Tag>
      },
    },
    {
      title: '备注',
      dataIndex: 'remark',
      key: 'remark',
      ellipsis: true,
    },
    {
      title: '操作',
      key: 'action',
      render: (_: unknown, record: Delegation) => {
        if (record.isRevoked) return null
        return (
          <Popconfirm
            title="确定要撤销此委托吗？"
            onConfirm={() => handleRevoke(record.id)}
            okText="撤销"
            cancelText="取消"
          >
            <Button type="link" danger size="small">
              <DeleteOutlined /> 撤销
            </Button>
          </Popconfirm>
        )
      },
    },
  ]

  const delegateColumns = [
    {
      title: '委托人',
      dataIndex: 'delegatorName',
      key: 'delegatorName',
    },
    {
      title: '委托开始',
      dataIndex: 'startDate',
      key: 'startDate',
      render: (v: string) => new Date(v).toLocaleString('zh-CN'),
    },
    {
      title: '委托结束',
      dataIndex: 'endDate',
      key: 'endDate',
      render: (v: string) => new Date(v).toLocaleString('zh-CN'),
    },
    {
      title: '状态',
      key: 'status',
      render: (_: unknown, record: Delegation) => {
        if (record.isRevoked) {
          return <Tag color="default">已撤销</Tag>
        }
        if (isActive(record)) {
          return <Tag color="success">生效中</Tag>
        }
        if (new Date(record.startDate) > new Date()) {
          return <Tag color="blue">待生效</Tag>
        }
        return <Tag color="default">已过期</Tag>
      },
    },
    {
      title: '备注',
      dataIndex: 'remark',
      key: 'remark',
      ellipsis: true,
    },
  ]

  return (
    <div>
      <Card
        title={
          <Space>
            <UserSwitchOutlined />
            <span>审批委托管理</span>
          </Space>
        }
        extra={
          <Space>
            <Button
              icon={<ReloadOutlined />}
              onClick={() => {
                fetchMyDelegationsAsDelegator()
                fetchMyDelegationsAsDelegate()
                fetchActiveDelegation()
              }}
              loading={loading}
            >
              刷新
            </Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreateModal}>
              新建委托
            </Button>
          </Space>
        }
      >
        {activeDelegation && (
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              <span>
                您当前有一条生效中的委托：
                <strong>{activeDelegation.delegatorName}</strong>
                委托您代为审批，有效期至 {new Date(activeDelegation.endDate).toLocaleString('zh-CN')}
              </span>
            }
          />
        )}

        <Tabs
          items={[
            {
              key: 'as-delegator',
              label: '我委托的',
              children: (
                <Table
                  rowKey="id"
                  columns={delegatorColumns}
                  dataSource={myDelegationsAsDelegator}
                  loading={loading}
                  pagination={{ pageSize: 10 }}
                />
              ),
            },
            {
              key: 'as-delegate',
              label: '委托给我的',
              children: (
                <Table
                  rowKey="id"
                  columns={delegateColumns}
                  dataSource={myDelegationsAsDelegate}
                  loading={loading}
                  pagination={{ pageSize: 10 }}
                />
              ),
            },
          ]}
        />
      </Card>

      <Modal
        title="新建审批委托"
        open={createModalOpen}
        onOk={handleCreate}
        onCancel={() => setCreateModalOpen(false)}
        okText="创建委托"
        cancelText="取消"
        confirmLoading={loading}
        width={500}
      >
        {form && (
          <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
            <Form.Item
              label="被委托人"
              name="delegateId"
              rules={[{ required: true, message: '请选择被委托人' }]}
            >
              <Select
                placeholder="请选择被委托人"
                showSearch
                optionFilterProp="label"
                options={delegateOptions.map((u: { id: string; displayName: string; role: string; department: string }) => ({
                  value: u.id,
                  label: `${u.displayName} (${u.role}${u.department ? ' - ' + u.department : ''})`,
                }))}
              />
            </Form.Item>
            <Form.Item
              label="委托时间范围"
              name="dateRange"
              rules={[{ required: true, message: '请选择委托时间范围' }]}
            >
              <RangePicker
                showTime={{ format: 'HH:mm' }}
                format="YYYY-MM-DD HH:mm"
                style={{ width: '100%' }}
              />
            </Form.Item>
            <Form.Item label="备注" name="remark">
              <TextArea
                rows={3}
                placeholder="可选：填写委托原因或备注说明"
                maxLength={500}
              />
            </Form.Item>
            <Alert
              type="warning"
              showIcon
              message="委托规则"
              description={
                <ul style={{ margin: 0, paddingLeft: 20 }}>
                  <li>同一时间段内只能有一个生效的委托记录</li>
                  <li>委托不可传递：被委托人不能再委托给第三人</li>
                  <li>委托期间被委托人可以代替原审批人进行审批</li>
                </ul>
              }
            />
          </Form>
        )}
      </Modal>
    </div>
  )
}
