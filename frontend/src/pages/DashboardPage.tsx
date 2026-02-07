import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, Row, Col, Statistic, Table, Typography, Spin, Empty } from 'antd'
import { 
  ClockCircleOutlined, 
  FileTextOutlined, 
  DollarOutlined,
  CheckCircleOutlined,
  RightOutlined
} from '@ant-design/icons'
import { Column, Line } from '@ant-design/charts'
import { statisticsApi } from '../api'
import type { DashboardData } from '../types'
const { Title, Text } = Typography

export default function DashboardPage() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<DashboardData | null>(null)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const result = await statisticsApi.getDashboard()
        setData(result)
      } catch (error) {
        console.error('获取仪表盘数据失败', error)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [])

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 400 }}>
        <Spin size="large" />
      </div>
    )
  }

  if (!data) {
    return <Empty description="加载失败" />
  }

  // 近7天申请数量柱状图配置
  const columnConfig = {
    data: data.dailyTrend,
    xField: 'date',
    yField: 'count',
    color: '#1677FF',
    label: {
      position: 'top' as const,
      style: {
        fill: '#666',
        fontSize: 12,
      },
    },
    xAxis: {
      label: {
        style: { fontSize: 12 },
      },
    },
    yAxis: {
      label: {
        formatter: (v: string) => `${v}件`,
      },
    },
    columnStyle: {
      radius: [4, 4, 0, 0],
    },
    meta: {
      count: { alias: '申请数量' },
      date: { alias: '日期' },
    },
  }

  // 趋势折线图配置
  const lineConfig = {
    data: data.dailyTrend,
    xField: 'date',
    yField: 'amount',
    smooth: true,
    point: { size: 4 },
    color: '#1677FF',
    areaStyle: {
      fill: 'l(270) 0:#ffffff 0.5:#d6e4ff 1:#1677FF',
    },
    yAxis: {
      label: {
        formatter: (v: string) => `¥${Number(v).toLocaleString()}`,
      },
    },
  }

  // 待办列表列配置
  const todoColumns = [
    {
      title: '申请编号',
      dataIndex: 'requestNumber',
      key: 'requestNumber',
      render: (text: string) => <Text strong>{text}</Text>,
    },
    {
      title: '物品名称',
      dataIndex: 'itemName',
      key: 'itemName',
    },
    {
      title: '申请人',
      dataIndex: 'applicantName',
      key: 'applicantName',
    },
    {
      title: '金额',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (amount: number) => (
        <Text type="danger">¥{amount.toLocaleString()}</Text>
      ),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: any) => (
        <a onClick={() => navigate(`/requests/${record.requestId}`)}>
          去审批 <RightOutlined />
        </a>
      ),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 24 }}>仪表盘</Title>
      
      {/* 统计卡片 */}
      <Row gutter={[24, 24]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} style={{ borderRadius: 8 }}>
            <Statistic
              title="待我审批"
              value={data.pendingApprovalCount}
              prefix={<ClockCircleOutlined style={{ color: '#1677FF' }} />}
              valueStyle={{ color: '#1677FF' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} style={{ borderRadius: 8 }}>
            <Statistic
              title="我的申请"
              value={data.myRequestCount}
              prefix={<FileTextOutlined style={{ color: '#722ED1' }} />}
              valueStyle={{ color: '#722ED1' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} style={{ borderRadius: 8 }}>
            <Statistic
              title="本月采购总额"
              value={data.monthlyAmount}
              precision={2}
              prefix={<DollarOutlined style={{ color: '#52C41A' }} />}
              valueStyle={{ color: '#52C41A' }}
              formatter={(value) => `¥${Number(value).toLocaleString()}`}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} style={{ borderRadius: 8 }}>
            <Statistic
              title="审批通过率"
              value={data.approvalRate}
              precision={1}
              suffix="%"
              prefix={<CheckCircleOutlined style={{ color: '#FAAD14' }} />}
              valueStyle={{ color: '#FAAD14' }}
            />
          </Card>
        </Col>
      </Row>

      {/* 图表 */}
      <Row gutter={[24, 24]} style={{ marginBottom: 24 }}>
        <Col xs={24} lg={12}>
          <Card 
            title="近7天申请统计" 
            bordered={false} 
            style={{ borderRadius: 8, height: 400 }}
          >
            {data.dailyTrend.length > 0 ? (
              <Column {...columnConfig} height={280} />
            ) : (
              <Empty description="暂无数据" style={{ marginTop: 80 }} />
            )}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card 
            title="近期采购趋势" 
            bordered={false} 
            style={{ borderRadius: 8, height: 400 }}
          >
            {data.dailyTrend.length > 0 ? (
              <Line {...lineConfig} height={280} />
            ) : (
              <Empty description="暂无数据" style={{ marginTop: 80 }} />
            )}
          </Card>
        </Col>
      </Row>

      {/* 待办事项 */}
      <Card 
        title="待办事项" 
        bordered={false} 
        style={{ borderRadius: 8 }}
        extra={
          data.todoItems.length > 0 && (
            <a onClick={() => navigate('/approvals')}>查看全部</a>
          )
        }
      >
        <Table
          dataSource={data.todoItems}
          columns={todoColumns}
          rowKey="requestId"
          pagination={false}
          locale={{ emptyText: <Empty description="暂无待办事项" /> }}
        />
      </Card>
    </div>
  )
}
