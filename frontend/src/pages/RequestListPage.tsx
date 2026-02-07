import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, Table, Button, Tag, Space, Select, Typography, message, Popconfirm } from 'antd'
import { PlusOutlined, EyeOutlined, EditOutlined, SendOutlined, CloseCircleOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { requestsApi } from '../api'
import { useStore } from '../store/useStore'
import type { PurchaseRequest } from '../types'
import { RequestStatus, RequestStatusLabels, RequestStatusColors, UrgencyLabels } from '../types'

const { Title } = Typography

export default function RequestListPage() {
  const navigate = useNavigate()
  const user = useStore((state) => state.user)
  const [loading, setLoading] = useState(true)
  const [requests, setRequests] = useState<PurchaseRequest[]>([])
  const [filteredRequests, setFilteredRequests] = useState<PurchaseRequest[]>([])
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined)

  const fetchRequests = async () => {
    setLoading(true)
    try {
      const data = await requestsApi.getAll({ userId: user?.id })
      setRequests(data)
      setFilteredRequests(data)
    } catch (error: any) {
      message.error(error.message || '获取申请列表失败')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchRequests()
  }, [user?.id])

  useEffect(() => {
    let filtered = [...requests]
    if (statusFilter !== undefined) {
      filtered = filtered.filter(r => r.statusValue === statusFilter)
    }
    setFilteredRequests(filtered)
  }, [statusFilter, requests])

  const handleSubmit = async (id: string) => {
    try {
      await requestsApi.submit(id)
      message.success('提交成功')
      fetchRequests()
    } catch (error: any) {
      message.error(error.message || '提交失败')
    }
  }

  const handleCancel = async (id: string) => {
    try {
      await requestsApi.cancel(id)
      message.success('撤销成功')
      fetchRequests()
    } catch (error: any) {
      message.error(error.message || '撤销失败')
    }
  }

  const columns = [
    {
      title: '申请编号',
      dataIndex: 'requestNumber',
      key: 'requestNumber',
      width: 140,
      render: (text: string) => <a onClick={() => navigate(`/requests/${text}`)}>{text}</a>,
    },
    {
      title: '物品名称',
      dataIndex: 'itemName',
      key: 'itemName',
      ellipsis: true,
    },
    {
      title: '数量',
      dataIndex: 'quantity',
      key: 'quantity',
      width: 80,
      align: 'right' as const,
    },
    {
      title: '单价',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      width: 100,
      align: 'right' as const,
      render: (price: number) => `¥${price.toLocaleString()}`,
    },
    {
      title: '总金额',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      width: 120,
      align: 'right' as const,
      render: (amount: number) => (
        <span style={{ color: '#FF4D4F', fontWeight: 500 }}>
          ¥{amount.toLocaleString()}
        </span>
      ),
    },
    {
      title: '紧急程度',
      dataIndex: 'urgencyValue',
      key: 'urgency',
      width: 90,
      render: (urgency: number) => (
        <Tag color={urgency === 1 ? 'red' : 'default'}>
          {UrgencyLabels[urgency as keyof typeof UrgencyLabels]}
        </Tag>
      ),
    },
    {
      title: '状态',
      dataIndex: 'statusValue',
      key: 'status',
      width: 100,
      render: (status: RequestStatus) => (
        <Tag color={RequestStatusColors[status]}>
          {RequestStatusLabels[status]}
        </Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      render: (_: any, record: PurchaseRequest) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/requests/${record.id}`)}
          >
            详情
          </Button>
          {(record.statusValue === RequestStatus.Draft || record.statusValue === RequestStatus.Returned) && (
            <>
              <Button
                type="link"
                size="small"
                icon={<EditOutlined />}
                onClick={() => navigate(`/requests/${record.id}/edit`)}
              >
                编辑
              </Button>
              <Popconfirm
                title="确定提交申请？"
                onConfirm={() => handleSubmit(record.id)}
              >
                <Button type="link" size="small" icon={<SendOutlined />}>
                  提交
                </Button>
              </Popconfirm>
            </>
          )}
          {(record.statusValue === RequestStatus.Pending || 
            record.statusValue === RequestStatus.ManagerApproved ||
            record.statusValue === RequestStatus.FinanceApproved) && (
            <Popconfirm
              title="确定撤销申请？"
              onConfirm={() => handleCancel(record.id)}
            >
              <Button type="link" size="small" danger icon={<CloseCircleOutlined />}>
                撤销
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={4} style={{ margin: 0 }}>采购申请</Title>
        <Button 
          type="primary" 
          icon={<PlusOutlined />}
          onClick={() => navigate('/requests/new')}
        >
          新建申请
        </Button>
      </div>

      <Card bordered={false} style={{ borderRadius: 8 }}>
        {/* 筛选条件 */}
        <div style={{ marginBottom: 16 }}>
          <Space wrap>
            <Select
              placeholder="状态筛选"
              allowClear
              style={{ width: 140 }}
              value={statusFilter}
              onChange={setStatusFilter}
              options={Object.entries(RequestStatusLabels).map(([value, label]) => ({
                value: Number(value),
                label,
              }))}
            />
          </Space>
        </div>

        {/* 表格 */}
        <Table
          dataSource={filteredRequests}
          columns={columns}
          rowKey="id"
          loading={loading}
          pagination={{
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total) => `共 ${total} 条`,
          }}
        />
      </Card>
    </div>
  )
}
