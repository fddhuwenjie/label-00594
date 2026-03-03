import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, Form, Input, Button, Typography, Space, message } from 'antd'
import { UserOutlined, LockOutlined, LoginOutlined } from '@ant-design/icons'
import { useStore } from '../store/useStore'
import { authApi } from '../api'

const { Title, Text } = Typography

export default function LoginPage() {
  const navigate = useNavigate()
  const { user, setSession } = useStore()
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    // 如果已登录，跳转到首页
    if (user) {
      navigate('/')
    }
  }, [user, navigate])

  const handleLogin = async (values: { username: string; password: string }) => {
    setLoading(true)
    try {
      const loginResult = await authApi.login(values.username.trim(), values.password)
      setSession({
        user: {
          id: loginResult.id,
          username: loginResult.username,
          displayName: loginResult.displayName,
          role: loginResult.role,
          roleValue: loginResult.roleValue,
          department: loginResult.department,
          token: loginResult.token,
          tokenType: loginResult.tokenType,
          expiresAt: loginResult.expiresAt,
        },
        token: loginResult.token,
      })
      message.success(`欢迎，${loginResult.displayName}！`)
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
            <Text type="secondary">请输入账号和密码登录系统</Text>
          </div>

          <Form layout="vertical" onFinish={handleLogin}>
            <Form.Item
              name="username"
              label="账号"
              rules={[{ required: true, message: '请输入账号' }]}
            >
              <Input
                size="large"
                prefix={<UserOutlined />}
                placeholder="请输入账号"
                autoComplete="username"
              />
            </Form.Item>
            <Form.Item
              name="password"
              label="密码"
              rules={[{ required: true, message: '请输入密码' }]}
            >
              <Input.Password
                size="large"
                prefix={<LockOutlined />}
                placeholder="请输入密码"
                autoComplete="current-password"
              />
            </Form.Item>
            <Form.Item style={{ marginBottom: 0 }}>
              <Button
                type="primary"
                size="large"
                block
                icon={<LoginOutlined />}
                loading={loading}
                htmlType="submit"
              >
                登 录
              </Button>
            </Form.Item>
          </Form>

        </Space>
      </Card>
    </div>
  )
}
