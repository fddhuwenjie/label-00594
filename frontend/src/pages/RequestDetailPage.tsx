import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { 
  Card, Descriptions, Tag, Timeline, Button, Space, Typography, 
  Spin, Empty, message, Popconfirm, Modal, Input 
} from 'antd'
import { 
  ArrowLeftOutlined, EditOutlined, SendOutlined, CloseCircleOutlined,
  CheckCircleOutlined, CloseOutlined, RollbackOutlined
} from '@ant-design/icons'
import dayjs from 'dayjs'
import { requestsApi, approvalsApi } from '../api'
import { useStore } from '../store/useStore'
import type { PurchaseRequest } from '../types'
import { 
  RequestStatus, RequestStatusLabels, RequestStatusColors, 
  UrgencyLabels, ApprovalAction, ApprovalActionLabels, UserRole 
} from '../types'
import RequestFormModal from '../components/RequestFormModal'

const { Title, Text } = Typography
const { TextArea } = Input

export default function RequestDetailPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const user = useStore((state) => state.user)
  const [loading, setLoading] = useState(true)
  const [request, setRequest] = useState<PurchaseRequest | null>(null)
  const [approvalModalOpen, setApprovalModalOpen] = useState(false)
  const [approvalAction, setApprovalAction] = useState<ApprovalAction | null>(null)
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [editModalOpen, setEditModalOpen] = useState(false)

  const fetchRequest = async () => {
    setLoading(true)
    try {
      const data = await requestsApi.getById(id!)
      setRequest(data)
    } catch (error: any) {
      message.error(error.message || '获取申请详情失败')
      navigate('/requests')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchRequest()
  }, [id])

  const handleSubmit = async () => {
    try {
      await requestsApi.submit(id!)
      message.success('提交成功')
      fetchRequest()
    } catch (error: any) {
      message.error(error.message || '提交失败')
    }
  }

  const handleCancel = async () => {
    try {
      await requestsApi.cancel(id!)
      message.success('撤销成功')
      fetchRequest()
    } catch (error: any) {
      message.error(error.message || '撤销失败')
    }
  }

  const openApprovalModal = (action: ApprovalAction) => {
    setApprovalAction(action)
    setComment('')
    setApprovalModalOpen(true)
  }

  const handleApproval = async () => {
    if (approvalAction === null) return

    setSubmitting(true)
    try {
      switch (approvalAction) {
        case ApprovalAction.Approve:
          await approvalsApi.approve(id!, comment)
          message.success('审批通过')
          break
        case ApprovalAction.Reject:
          await approvalsApi.reject(id!, comment)
          message.success('已拒绝')
          break
        case ApprovalAction.Return:
          await approvalsApi.return(id!, comment)
          message.success('已退回')
          break
      }
      setApprovalModalOpen(false)
      fetchRequest()
    } catch (error: any) {
      message.error(error.message || '操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 判断当前用户是否可以审批
  const canApprove = () => {
    if (!user || !request) return false
    
    return (
      (user.roleValue === UserRole.Manager && request.statusValue === RequestStatus.Pending && request.currentApprovalLevel === 1) ||
      (user.roleValue === UserRole.Finance && request.statusValue === RequestStatus.ManagerApproved && request.currentApprovalLevel === 2) ||
      (user.roleValue === UserRole.Director && request.statusValue === RequestStatus.FinanceApproved && request.currentApprovalLevel === 3)
    )
  }

  // 判断是否是申请人
  const isApplicant = user?.id === request?.applicantId

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: 400 }}>
        <Spin size="large" />
      </div>
    )
  }

  if (!request) {
    return <Empty description="申请不存在" />
  }

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <Button 
            type="text" 
            icon={<ArrowLeftOutlined />} 
            onClick={() => navigate(-1)}
            style={{ marginRight: 8 }}
          />
          <Title level={4} style={{ margin: 0 }}>
            申请详情
          </Title>
          <Text type="secondary" style={{ marginLeft: 16 }}>
            {request.requestNumber}
          </Text>
        </div>
        <Space>
          {/* 申请人操作 */}
          {isApplicant && (request.statusValue === RequestStatus.Draft || request.statusValue === RequestStatus.Returned) && (
            <>
              <Button icon={<EditOutlined />} onClick={() => setEditModalOpen(true)}>
                编辑
              </Button>
              <Popconfirm title="确定提交申请？" onConfirm={handleSubmit}>
                <Button type="primary" icon={<SendOutlined />}>提交</Button>
              </Popconfirm>
            </>
          )}
          {isApplicant && (request.statusValue === RequestStatus.Pending || 
            request.statusValue === RequestStatus.ManagerApproved ||
            request.statusValue === RequestStatus.FinanceApproved) && (
            <Popconfirm title="确定撤销申请？" onConfirm={handleCancel}>
              <Button danger icon={<CloseCircleOutlined />}>撤销</Button>
            </Popconfirm>
          )}
          {/* 审批人操作 */}
          {canApprove() && (
            <>
              <Button icon={<RollbackOutlined />} onClick={() => openApprovalModal(ApprovalAction.Return)}>
                退回
              </Button>
              <Button danger icon={<CloseOutlined />} onClick={() => openApprovalModal(ApprovalAction.Reject)}>
                拒绝
              </Button>
              <Button type="primary" icon={<CheckCircleOutlined />} onClick={() => openApprovalModal(ApprovalAction.Approve)}>
                通过
              </Button>
            </>
          )}
        </Space>
      </div>

      <div style={{ display: 'flex', gap: 24 }}>
        {/* 基本信息 */}
        <Card bordered={false} style={{ borderRadius: 8, flex: 1 }} title="基本信息">
          <Descriptions column={2}>
            <Descriptions.Item label="申请编号">{request.requestNumber}</Descriptions.Item>
            <Descriptions.Item label="状态">
              <Tag color={RequestStatusColors[request.statusValue]}>
                {RequestStatusLabels[request.statusValue]}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="申请人">{request.applicantName}</Descriptions.Item>
            <Descriptions.Item label="所属部门">{request.applicantDepartment}</Descriptions.Item>
            <Descriptions.Item label="物品名称">{request.itemName}</Descriptions.Item>
            <Descriptions.Item label="紧急程度">
              <Tag color={request.urgencyValue === 1 ? 'red' : 'default'}>
                {UrgencyLabels[request.urgencyValue]}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="采购数量">{request.quantity}</Descriptions.Item>
            <Descriptions.Item label="单价">¥{request.unitPrice.toLocaleString()}</Descriptions.Item>
            <Descriptions.Item label="总金额">
              <span style={{ color: '#FF4D4F', fontWeight: 600, fontSize: 16 }}>
                ¥{request.totalAmount.toLocaleString()}
              </span>
            </Descriptions.Item>
            <Descriptions.Item label="当前审批级别">
              {request.currentApprovalLevel === 0 ? '未提交' : `第 ${request.currentApprovalLevel} 级`}
            </Descriptions.Item>
            <Descriptions.Item label="创建时间" span={2}>
              {dayjs(request.createdAt).format('YYYY-MM-DD HH:mm:ss')}
            </Descriptions.Item>
            <Descriptions.Item label="采购原因" span={2}>
              {request.reason || '-'}
            </Descriptions.Item>
          </Descriptions>
        </Card>

        {/* 审批流程 */}
        <Card bordered={false} style={{ borderRadius: 8, width: 360 }} title="审批流程">
          {request.approvalRecords && request.approvalRecords.length > 0 ? (
            <Timeline
              items={request.approvalRecords.map((record) => ({
                color: record.actionValue === ApprovalAction.Approve ? 'green' : 
                       record.actionValue === ApprovalAction.Reject ? 'red' : 'orange',
                children: (
                  <div>
                    <div style={{ fontWeight: 500 }}>
                      {record.approverName}
                      <Tag 
                        color={record.actionValue === ApprovalAction.Approve ? 'success' : 
                               record.actionValue === ApprovalAction.Reject ? 'error' : 'warning'}
                        style={{ marginLeft: 8 }}
                      >
                        {ApprovalActionLabels[record.actionValue]}
                      </Tag>
                    </div>
                    <div style={{ color: '#8C8C8C', fontSize: 12, marginTop: 4 }}>
                      {dayjs(record.createdAt).format('YYYY-MM-DD HH:mm')}
                    </div>
                    {record.comment && (
                      <div style={{ color: '#595959', marginTop: 4, fontSize: 13 }}>
                        {record.comment}
                      </div>
                    )}
                  </div>
                ),
              }))}
            />
          ) : (
            <Empty description="暂无审批记录" image={Empty.PRESENTED_IMAGE_SIMPLE} />
          )}
        </Card>
      </div>

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
        <div style={{ marginBottom: 16 }}>
          <Text type="secondary">
            {approvalAction === ApprovalAction.Approve && '确定通过此采购申请？'}
            {approvalAction === ApprovalAction.Reject && '确定拒绝此采购申请？申请将终止。'}
            {approvalAction === ApprovalAction.Return && '确定退回此采购申请？申请人可修改后重新提交。'}
          </Text>
        </div>
        <TextArea
          placeholder="请输入审批意见（选填）"
          rows={3}
          value={comment}
          onChange={(e) => setComment(e.target.value)}
        />
      </Modal>

      <RequestFormModal
        open={editModalOpen}
        editId={id ?? null}
        onClose={() => setEditModalOpen(false)}
        onSuccess={() => {
          setEditModalOpen(false)
          fetchRequest()
        }}
      />
    </div>
  )
}
