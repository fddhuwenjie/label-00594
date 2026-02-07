import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, List, Typography, Button, Tag, Empty, Spin, message, Space } from 'antd'
import { BellOutlined, CheckOutlined, EyeOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'
import { notificationsApi } from '../api'
import { useStore } from '../store/useStore'
import type { Notification } from '../types'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const { Title, Text, Paragraph } = Typography

export default function NotificationPage() {
  const navigate = useNavigate()
  const setUnreadCount = useStore((state) => state.setUnreadCount)
  const [loading, setLoading] = useState(true)
  const [notifications, setNotifications] = useState<Notification[]>([])

  const fetchNotifications = async () => {
    setLoading(true)
    try {
      const data = await notificationsApi.getAll()
      setNotifications(data)
      
      // 更新未读数量
      const unreadCount = data.filter(n => !n.isRead).length
      setUnreadCount(unreadCount)
    } catch (error: any) {
      message.error(error.message || '获取通知失败')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchNotifications()
  }, [])

  const handleMarkAsRead = async (id: string) => {
    try {
      await notificationsApi.markAsRead(id)
      setNotifications(prev =>
        prev.map(n => (n.id === id ? { ...n, isRead: true } : n))
      )
      setUnreadCount(notifications.filter(n => !n.isRead && n.id !== id).length)
    } catch (error: any) {
      message.error(error.message || '操作失败')
    }
  }

  const handleMarkAllAsRead = async () => {
    try {
      await notificationsApi.markAllAsRead()
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
      setUnreadCount(0)
      message.success('已全部标记为已读')
    } catch (error: any) {
      message.error(error.message || '操作失败')
    }
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <Title level={4} style={{ margin: 0 }}>通知中心</Title>
          {unreadCount > 0 && (
            <Text type="secondary">您有 {unreadCount} 条未读通知</Text>
          )}
        </div>
        {unreadCount > 0 && (
          <Button icon={<CheckOutlined />} onClick={handleMarkAllAsRead}>
            全部标记已读
          </Button>
        )}
      </div>

      <Card bordered={false} style={{ borderRadius: 8 }}>
        {loading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
            <Spin size="large" />
          </div>
        ) : notifications.length === 0 ? (
          <Empty 
            image={<BellOutlined style={{ fontSize: 64, color: '#D9D9D9' }} />}
            description="暂无通知" 
          />
        ) : (
          <List
            itemLayout="horizontal"
            dataSource={notifications}
            renderItem={(item) => (
              <List.Item
                style={{
                  background: item.isRead ? 'transparent' : '#E6F4FF',
                  padding: '16px 20px',
                  marginBottom: 8,
                  borderRadius: 8,
                  cursor: 'pointer',
                }}
                onClick={() => {
                  if (!item.isRead) {
                    handleMarkAsRead(item.id)
                  }
                  if (item.requestId) {
                    navigate(`/requests/${item.requestId}`)
                  }
                }}
              >
                <List.Item.Meta
                  avatar={
                    <div style={{
                      width: 40,
                      height: 40,
                      borderRadius: 20,
                      background: item.isRead ? '#F0F2F5' : '#1677FF',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}>
                      <BellOutlined style={{ color: item.isRead ? '#8C8C8C' : '#fff' }} />
                    </div>
                  }
                  title={
                    <Space>
                      <Text strong={!item.isRead}>{item.title}</Text>
                      {!item.isRead && <Tag color="processing">未读</Tag>}
                    </Space>
                  }
                  description={
                    <div>
                      <Paragraph 
                        style={{ marginBottom: 4, color: '#595959' }}
                        ellipsis={{ rows: 2 }}
                      >
                        {item.content}
                      </Paragraph>
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        {dayjs(item.createdAt).fromNow()}
                      </Text>
                    </div>
                  }
                />
                {item.requestId && (
                  <Button 
                    type="link" 
                    icon={<EyeOutlined />}
                    onClick={(e) => {
                      e.stopPropagation()
                      navigate(`/requests/${item.requestId}`)
                    }}
                  >
                    查看申请
                  </Button>
                )}
              </List.Item>
            )}
          />
        )}
      </Card>
    </div>
  )
}
