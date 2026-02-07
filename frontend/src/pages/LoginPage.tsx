import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, Select, Button, Typography, Space, message } from 'antd'
import { UserOutlined, LoginOutlined } from '@ant-design/icons'
import { useStore } from '../store/useStore'
import { usersApi, authApi } from '../api'
import type { User } from '../types'
import { UserRoleLabels } from '../types'

const { Title, Text } = Typography

export default function LoginPage() {
  const navigate = useNavigate()
  const { user, setUser } = useStore()
  const [users, setUsers] = useState<User[]>([])
  const [selectedUser, setSelectedUser] = useState<string>('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    // 如果已登录，跳转到首页
    if (user) {
      navigate('/')
      return
    }

    // 获取用户列表
    const fetchUsers = async () => {
      try {
        const data = await usersApi.getAll()
        setUsers(data)
      } catch (error) {
        message.error('获取用户列表失败')
      }
    }
    fetchUsers()
  }, [user, navigate])

  const handleLogin = async () => {
    if (!selectedUser) {
      message.warning('请选择用户')
      return
    }

    setLoading(true)
    try {
      const userData = await authApi.login(selectedUser)
      setUser(userData)
      message.success(`欢迎，${userData.displayName}！`)
      navigate('/')
    } catch (error: any) {
      message.error(error.message || '登录失败')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #1677FF 0%, #69B1FF 100%)',
    }}>
      <Card
        style={{
          width: 400,
          boxShadow: '0 8px 24px rgba(0,0,0,0.15)',
          borderRadius: 12,
        }}
        bordered={false}
      >
        <Space direction="vertical" size={24} style={{ width: '100%' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{
              width: 64,
              height: 64,
              borderRadius: 12,
              background: 'linear-gradient(135deg, #1677FF 0%, #69B1FF 100%)',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: 16,
            }}>
              <UserOutlined style={{ fontSize: 32, color: '#fff' }} />
            </div>
            <Title level={3} style={{ margin: 0, color: '#141414' }}>
              采购审批管理系统
            </Title>
            <Text type="secondary">选择用户登录系统</Text>
          </div>

          <Select
            size="large"
            placeholder="请选择用户"
            style={{ width: '100%' }}
            value={selectedUser || undefined}
            onChange={setSelectedUser}
            options={users.map(u => ({
              value: u.username,
              label: (
                <Space>
                  <span>{u.displayName}</span>
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {UserRoleLabels[u.roleValue]} · {u.department}
                  </Text>
                </Space>
              ),
            }))}
          />

          <Button
            type="primary"
            size="large"
            block
            icon={<LoginOutlined />}
            loading={loading}
            onClick={handleLogin}
          >
            登 录
          </Button>

          <div style={{ 
            padding: 16, 
            background: '#F0F2F5', 
            borderRadius: 8,
            fontSize: 12,
            color: '#8C8C8C'
          }}>
            <div style={{ marginBottom: 8, color: '#595959', fontWeight: 500 }}>测试账号说明：</div>
            <div>• 张三 - 普通员工，可创建申请</div>
            <div>• 李四 - 部门经理，审批≤5000元申请</div>
            <div>• 王五 - 财务总监，审批≤20000元申请</div>
            <div>• 赵六 - 总经理，审批所有申请</div>
            <div>• admin - 系统管理员</div>
          </div>
        </Space>
      </Card>
    </div>
  )
}
