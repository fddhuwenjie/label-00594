import { useState, useEffect } from 'react'
import {
  Card, Table, Tag, Button, Typography, Space, Spin, Modal, Form,
  Select, DatePicker, Input, message, Descriptions, Empty, Popconfirm,
} from 'antd'
import {
  SwapOutlined, PlusOutlined, StopOutlined, InfoCircleOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { useStore } from '../store/useStore'
import { useDelegationStore } from '../store/useDelegationStore'
import type { Delegation } from '../types'
import {
  UserRole, UserRoleLabels, DelegationStatus, DelegationStatusLabels,
  DelegationStatusColors,
} from '../types'

const { Title, Text } = Typography
const { RangePicker } = DatePicker

export default function DelegationPage() {
  const user = useStore((state) => state.user)
  const {
    delegationsAsDelegator, delegationsAsDelegatee, activeDelegation,
    approvers, loading, fetchMyDelegations, fetchActiveDelegation,
    fetchApprovers, createDelegation, revokeDelegation,
  } = useDelegationStore()

  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [form] = Form.useForm()

  useEffect(() => {
    fetchMyDelegations()
    fetchActiveDelegation()
    fetchApprovers()
  }, [])

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const success = await createDelegation({
        delegateeId: values.delegateeId,
        startDate: values.dateRange[0].startOf('day').toISOString(),
        endDate: values.dateRange[1].endOf('day').toISOString(),
        reason: values.reason,
      })

      if (success) {
        message.success('委托创建成功')
        setCreateModalOpen(false)
        form.resetFields()
      } else {
        message.error('创建委托失败，请检查业务约束')
      }
    } catch (error: any) {
      if (error.message) {
        message.error(error.message)
      }
    } finally {
      setSubmitting(false)
    }
  }

  const handleRevoke = async (id: string) => {
    const success = await revokeDelegation(id)
    if (success) {
      message.success('委托已撤销')
    } else {
      message.error('撤销委托失败')
    }
  }

  if (user?.roleValue === UserRole.Employee || user?.roleValue === UserRole.Admin) {
    return (
      <div>
        <Title level={4} style={{ marginBottom: 24 }}>审批委托管理</Title>
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Empty description="您没有审批权限，无需管理委托" />
        </Card>
      </div>
    )
  }

  const delegatorColumns = [
    {
      title: '被委托人',
      dataIndex: 'delegateeName',
      key: 'delegateeName',
      width: 120,
    },
    {
      title: '开始日期',
      dataIndex: 'startDate',
      key: 'startDate',
      width: 120,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '结束日期',
      dataIndex: 'endDate',
      key: 'endDate',
      width: 120,
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
      dataIndex: 'statusValue',
      key: 'status',
      width: 100,
      render: (status: DelegationStatus) => (
        <Tag color={DelegationStatusColors[status]}>
          {DelegationStatusLabels[status]}
        </Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: unknown, record: Delegation) =>
        record.statusValue === DelegationStatus.Active ? (
          <Popconfirm
            title="确定撤销此委托？"
            onConfirm={() => handleRevoke(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" danger icon={<StopOutlined />} size="small">
              撤销
            </Button>
          </Popconfirm>
        ) : (
          <Text type="secondary">-</Text>
        ),
    },
  ]

  const delegateeColumns = [
    {
      title: '委托人',
      dataIndex: 'delegatorName',
      key: 'delegatorName',
      width: 120,
    },
    {
      title: '开始日期',
      dataIndex: 'startDate',
      key: 'startDate',
      width: 120,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '结束日期',
      dataIndex: 'endDate',
      key: 'endDate',
      width: 120,
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
      dataIndex: 'statusValue',
      key: 'status',
      width: 100,
      render: (status: DelegationStatus) => (
        <Tag color={DelegationStatusColors[status]}>
          {DelegationStatusLabels[status]}
        </Tag>
      ),
    },
  ]

  const approverOptions = approvers.map((a) => ({
    label: `${a.displayName} (${UserRoleLabels[a.role as keyof typeof UserRoleLabels] || a.role} - ${a.department})`,
    value: a.id,
  }))

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={4} style={{ margin: 0 }}>审批委托管理</Title>
        <Space>
          {activeDelegation && (
            <Tag color="success" style={{ fontSize: 13, padding: '4px 12px' }}>
              当前委托生效中 → {activeDelegation.delegateeName}
            </Tag>
          )}
          {!activeDelegation && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setCreateModalOpen(true)}
            >
              新建委托
            </Button>
          )}
        </Space>
      </div>

      {activeDelegation && (
        <Card
          bordered={false}
          style={{ borderRadius: 8, marginBottom: 16, background: '#F6FFED', borderColor: '#B7EB8F' }}
        >
          <Descriptions
            size="small"
            column={4}
            title={
              <Space>
                <SwapOutlined style={{ color: '#52C41A' }} />
                <Text strong>当前生效委托</Text>
              </Space>
            }
          >
            <Descriptions.Item label="被委托人">{activeDelegation.delegateeName}</Descriptions.Item>
            <Descriptions.Item label="委托期间">
              {dayjs(activeDelegation.startDate).format('YYYY-MM-DD')} ~ {dayjs(activeDelegation.endDate).format('YYYY-MM-DD')}
            </Descriptions.Item>
            <Descriptions.Item label="委托原因">{activeDelegation.reason || '-'}</Descriptions.Item>
            <Descriptions.Item>
              <Popconfirm
                title="确定撤销此委托？撤销后将恢复本人审批权限。"
                onConfirm={() => handleRevoke(activeDelegation.id)}
                okText="确定"
                cancelText="取消"
              >
                <Button danger size="small" icon={<StopOutlined />}>
                  撤销委托
                </Button>
              </Popconfirm>
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}

      <Card
        bordered={false}
        style={{ borderRadius: 8, marginBottom: 16 }}
        title={<Text strong>我发出的委托</Text>}
      >
        {loading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
            <Spin />
          </div>
        ) : delegationsAsDelegator.length === 0 ? (
          <Empty description="暂无委托记录" image={Empty.PRESENTED_IMAGE_SIMPLE} />
        ) : (
          <Table
            dataSource={delegationsAsDelegator}
            columns={delegatorColumns}
            rowKey="id"
            pagination={false}
            size="small"
          />
        )}
      </Card>

      <Card
        bordered={false}
        style={{ borderRadius: 8, marginBottom: 16 }}
        title={<Text strong>我接收的委托</Text>}
      >
        {loading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 24 }}>
            <Spin />
          </div>
        ) : delegationsAsDelegatee.length === 0 ? (
          <Empty description="暂无接收的委托" image={Empty.PRESENTED_IMAGE_SIMPLE} />
        ) : (
          <Table
            dataSource={delegationsAsDelegatee}
            columns={delegateeColumns}
            rowKey="id"
            pagination={false}
            size="small"
          />
        )}
      </Card>

      <Card bordered={false} style={{ borderRadius: 8 }} size="small">
        <div style={{ color: '#8C8C8C', fontSize: 13 }}>
          <div style={{ marginBottom: 8, color: '#595959', fontWeight: 500 }}>
            <InfoCircleOutlined style={{ marginRight: 4 }} />
            委托规则说明：
          </div>
          <div>• 同一时间段内一个审批人只能有一个生效的委托记录</div>
          <div>• 委托不可传递：被委托人不能再将审批权限委托给第三人</div>
          <div>• 委托期间，被委托人可代替原审批人进行审批操作</div>
          <div>• 审批记录中会同时记录原审批人和实际操作人</div>
          <div>• 委托到期后自动失效，也可手动提前撤销</div>
        </div>
      </Card>

      <Modal
        title="新建审批委托"
        open={createModalOpen}
        onCancel={() => {
          setCreateModalOpen(false)
          form.resetFields()
        }}
        onOk={handleCreate}
        confirmLoading={submitting}
        okText="创建"
        cancelText="取消"
        width={520}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="delegateeId"
            label="被委托人"
            rules={[{ required: true, message: '请选择被委托人' }]}
          >
            <Select
              placeholder="请选择被委托人"
              options={approverOptions}
              showSearch
              filterOption={(input, option) =>
                (option?.label as string)?.toLowerCase().includes(input.toLowerCase())
              }
            />
          </Form.Item>
          <Form.Item
            name="dateRange"
            label="委托期间"
            rules={[{ required: true, message: '请选择委托期间' }]}
          >
            <RangePicker
              style={{ width: '100%' }}
              disabledDate={(current) => current && current < dayjs().startOf('day')}
            />
          </Form.Item>
          <Form.Item
            name="reason"
            label="委托原因"
            rules={[{ max: 500, message: '委托原因不能超过 500 字' }]}
          >
            <Input.TextArea placeholder="请输入委托原因（选填）" rows={3} />
          </Form.Item>
        </Form>

        <div style={{ background: '#FFF7E6', padding: 12, borderRadius: 6, fontSize: 13, color: '#AD6800' }}>
          <InfoCircleOutlined style={{ marginRight: 4 }} />
          提示：同一时间段内只能有一个生效委托。委托创建后，被委托人可在委托期间代替您进行审批。
        </div>
      </Modal>
    </div>
  )
}
