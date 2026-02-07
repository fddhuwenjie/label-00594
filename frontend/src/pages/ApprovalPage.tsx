import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, List, Tag, Button, Space, Typography, Empty, Spin, Modal, Input, message } from 'antd'
import { 
  CheckCircleOutlined, CloseCircleOutlined, RollbackOutlined, 
  EyeOutlined, UserOutlined, DollarOutlined 
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { approvalsApi } from '../api'
import { useStore } from '../store/useStore'
import type { PurchaseRequest } from '../types'
import { UrgencyLabels, ApprovalAction, ApprovalActionLabels, UserRole } from '../types'

const { Title, Text, Paragraph } = Typography
const { TextArea } = Input

export default function ApprovalPage() {
  const navigate = useNavigate()
  const user = useStore((state) => state.user)
  const [loading, setLoading] = useState(true)
  const [requests, setRequests] = useState<PurchaseRequest[]>([])
  const [approvalModalOpen, setApprovalModalOpen] = useState(false)
  const [currentRequest, setCurrentRequest] = useState<PurchaseRequest | null>(null)
  const [approvalAction, setApprovalAction] = useState<ApprovalAction | null>(null)
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const fetchPendingApprovals = async () => {
    setLoading(true)
    try {
      const data = await approvalsApi.getPending()
      setRequests(data)
    } catch (error: any) {
      message.error(error.message || '获取待审批列表失败')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchPendingApprovals()
  }, [])

  const openApprovalModal = (request: PurchaseRequest, action: ApprovalAction) => {
    setCurrentRequest(request)
    setApprovalAction(action)
    setComment('')
    setApprovalModalOpen(true)
  }

  const handleApproval = async () => {
    if (!currentRequest || approvalAction === null) return

    setSubmitting(true)
    try {
      switch (approvalAction) {
        case ApprovalAction.Approve:
          await approvalsApi.approve(currentRequest.id, comment)
          message.success('审批通过')
          break
        case ApprovalAction.Reject:
          await approvalsApi.reject(currentRequest.id, comment)
          message.success('已拒绝')
          break
        case ApprovalAction.Return:
          await approvalsApi.return(currentRequest.id, comment)
          message.success('已退回')
          break
      }
      setApprovalModalOpen(false)
      fetchPendingApprovals()
    } catch (error: any) {
      message.error(error.message || '操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 获取审批级别说明
  const getApprovalLevelText = () => {
    switch (user?.roleValue) {
      case UserRole.Manager:
        return '部门经理审批 (第1级)'
      case UserRole.Finance:
        return '财务总监审批 (第2级)'
      case UserRole.Director:
        return '总经理审批 (第3级)'
      default:
        return ''
    }
  }

  if (user?.roleValue === UserRole.Employee) {
    return (
      <div>
        <Title level={4} style={{ marginBottom: 24 }}>审批工作台</Title>
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Empty description="您没有审批权限" />
        </Card>
      </div>
    )
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <Title level={4} style={{ margin: 0 }}>审批工作台</Title>
          <Text type="secondary">{getApprovalLevelText()}</Text>
        </div>
        <Tag color="processing" style={{ fontSize: 14, padding: '4px 12px' }}>
          待审批: {requests.length}
        </Tag>
      </div>

      <Card bordered={false} style={{ borderRadius: 8 }}>
        {loading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
            <Spin size="large" />
          </div>
        ) : requests.length === 0 ? (
          <Empty description="暂无待审批的申请" />
        ) : (
          <List
            itemLayout="vertical"
            dataSource={requests}
            renderItem={(item) => (
              <List.Item
                key={item.id}
                style={{
                  background: '#FAFAFA',
                  borderRadius: 8,
                  padding: 20,
                  marginBottom: 16,
                }}
                actions={[
                  <Button
                    key="view"
                    type="link"
                    icon={<EyeOutlined />}
                    onClick={() => navigate(`/requests/${item.id}`)}
                  >
                    查看详情
                  </Button>,
                  <Button
                    key="return"
                    icon={<RollbackOutlined />}
                    onClick={() => openApprovalModal(item, ApprovalAction.Return)}
                  >
                    退回
                  </Button>,
                  <Button
                    key="reject"
                    danger
                    icon={<CloseCircleOutlined />}
                    onClick={() => openApprovalModal(item, ApprovalAction.Reject)}
                  >
                    拒绝
                  </Button>,
                  <Button
                    key="approve"
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    onClick={() => openApprovalModal(item, ApprovalAction.Approve)}
                  >
                    通过
                  </Button>,
                ]}
              >
                <List.Item.Meta
                  title={
                    <Space>
                      <Text strong style={{ fontSize: 16 }}>{item.requestNumber}</Text>
                      <Text>-</Text>
                      <Text strong style={{ fontSize: 16 }}>{item.itemName}</Text>
                      <Tag color={item.urgencyValue === 1 ? 'red' : 'default'}>
                        {UrgencyLabels[item.urgencyValue]}
                      </Tag>
                    </Space>
                  }
                  description={
                    <Space size={24} style={{ marginTop: 8 }}>
                      <span>
                        <UserOutlined style={{ marginRight: 4 }} />
                        {item.applicantName} ({item.applicantDepartment})
                      </span>
                      <span>
                        <DollarOutlined style={{ marginRight: 4 }} />
                        <span style={{ color: '#FF4D4F', fontWeight: 600 }}>
                          ¥{item.totalAmount.toLocaleString()}
                        </span>
                      </span>
                      <span style={{ color: '#8C8C8C' }}>
                        {dayjs(item.createdAt).format('YYYY-MM-DD HH:mm')}
                      </span>
                    </Space>
                  }
                />
                {item.reason && (
                  <Paragraph 
                    ellipsis={{ rows: 2 }} 
                    style={{ marginTop: 12, marginBottom: 0, color: '#595959' }}
                  >
                    <Text type="secondary">申请原因：</Text>
                    {item.reason}
                  </Paragraph>
                )}
              </List.Item>
            )}
          />
        )}
      </Card>

      {/* 审批弹窗 */}
      <Modal
        title={`${approvalAction !== null ? ApprovalActionLabels[approvalAction] : ''}审批`}
        open={approvalModalOpen}
        onCancel={() => setApprovalModalOpen(false)}
        onOk={handleApproval}
        confirmLoading={submitting}
        okText="确定"
        cancelText="取消"
        okButtonProps={{
          danger: approvalAction === ApprovalAction.Reject,
        }}
      >
        {currentRequest && (
          <div style={{ marginBottom: 16 }}>
            <div style={{ background: '#F0F2F5', padding: 12, borderRadius: 6, marginBottom: 12 }}>
              <div><Text strong>{currentRequest.requestNumber}</Text> - {currentRequest.itemName}</div>
              <div style={{ marginTop: 4 }}>
                <Text type="secondary">金额：</Text>
                <Text strong style={{ color: '#FF4D4F' }}>¥{currentRequest.totalAmount.toLocaleString()}</Text>
              </div>
            </div>
            <Text type="secondary">
              {approvalAction === ApprovalAction.Approve && '确定通过此采购申请？'}
              {approvalAction === ApprovalAction.Reject && '确定拒绝此采购申请？申请将终止。'}
              {approvalAction === ApprovalAction.Return && '确定退回此采购申请？申请人可修改后重新提交。'}
            </Text>
          </div>
        )}
        <TextArea
          placeholder="请输入审批意见（选填）"
          rows={3}
          value={comment}
          onChange={(e) => setComment(e.target.value)}
        />
      </Modal>
    </div>
  )
}
