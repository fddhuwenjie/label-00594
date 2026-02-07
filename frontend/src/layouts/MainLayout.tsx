import { useState, useEffect } from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Avatar, Dropdown, Badge, Space, Typography } from 'antd'
import {
  DashboardOutlined,
  FileTextOutlined,
  AuditOutlined,
  BellOutlined,
  UserOutlined,
  LogoutOutlined,
  TeamOutlined,
} from '@ant-design/icons'
import { useStore } from '../store/useStore'
import { notificationsApi } from '../api'
import { UserRole } from '../types'

const { Header, Sider, Content } = Layout
const { Text } = Typography

export default function MainLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const { user, logout, unreadCount, setUnreadCount } = useStore()
  const [collapsed, setCollapsed] = useState(false)

  useEffect(() => {
    // 获取未读通知数量
    const fetchUnreadCount = async () => {
      try {
        const count = await notificationsApi.getUnreadCount()
        setUnreadCount(count)
      } catch (error) {
        console.error('获取未读通知失败', error)
      }
    }
    fetchUnreadCount()
    
    // 定时刷新
    const interval = setInterval(fetchUnreadCount, 30000)
    return () => clearInterval(interval)
  }, [setUnreadCount])

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  // 菜单项
  const menuItems = [
    {
      key: '/dashboard',
      icon: <DashboardOutlined />,
      label: '仪表盘',
    },
    {
      key: '/requests',
      icon: <FileTextOutlined />,
      label: '采购申请',
    },
    {
      key: '/approvals',
      icon: <AuditOutlined />,
      label: '审批工作台',
      // 只有审批角色可见
      hidden: user?.roleValue === UserRole.Employee,
    },
    {
      key: '/notifications',
      icon: <BellOutlined />,
      label: '通知中心',
    },
    {
      key: '/users',
      icon: <TeamOutlined />,
      label: '用户管理',
      // 只有管理员可见
      hidden: user?.roleValue !== UserRole.Admin,
    },
  ].filter(item => !item.hidden)

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: `${user?.displayName} (${user?.role})`,
      disabled: true,
    },
    { type: 'divider' as const },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      onClick: handleLogout,
    },
  ]

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider 
        collapsible 
        collapsed={collapsed} 
        onCollapse={setCollapsed}
        theme="dark"
      >
        <div style={{ 
          height: 64, 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          borderBottom: '1px solid rgba(255,255,255,0.1)'
        }}>
          <Text strong style={{ color: '#fff', fontSize: collapsed ? 14 : 16 }}>
            {collapsed ? '采购' : '采购审批管理'}
          </Text>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header style={{ 
          background: '#fff', 
          padding: '0 24px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          boxShadow: '0 1px 4px rgba(0,21,41,.08)'
        }}>
          <Space size={24}>
            <Badge count={unreadCount} size="small">
              <BellOutlined 
                style={{ fontSize: 18, cursor: 'pointer' }} 
                onClick={() => navigate('/notifications')}
              />
            </Badge>
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <Space style={{ cursor: 'pointer' }}>
                <Avatar icon={<UserOutlined />} style={{ backgroundColor: '#1677FF' }} />
                <Text>{user?.displayName}</Text>
              </Space>
            </Dropdown>
          </Space>
        </Header>
        <Content style={{ margin: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
