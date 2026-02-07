import { useState, useEffect } from 'react'
import { Card, Table, Tag, Typography, Spin, message } from 'antd'
import { UserOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { usersApi } from '../api'
import { useStore } from '../store/useStore'
import type { User } from '../types'
import { UserRole, UserRoleLabels } from '../types'

const { Title } = Typography

const roleColors: Record<UserRole, string> = {
  [UserRole.Employee]: 'default',
  [UserRole.Manager]: 'blue',
  [UserRole.Finance]: 'green',
  [UserRole.Director]: 'purple',
  [UserRole.Admin]: 'red',
}

export default function UserManagePage() {
  const user = useStore((state) => state.user)
  const [loading, setLoading] = useState(true)
  const [users, setUsers] = useState<User[]>([])

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const data = await usersApi.getAll()
        setUsers(data)
      } catch (error: any) {
        message.error(error.message || '获取用户列表失败')
      } finally {
        setLoading(false)
      }
    }
    fetchUsers()
  }, [])

  // 只有管理员可以访问
  if (user?.roleValue !== UserRole.Admin) {
    return (
      <div>
        <Title level={4} style={{ marginBottom: 24 }}>用户管理</Title>
        <Card bordered={false} style={{ borderRadius: 8, textAlign: 'center', padding: 48 }}>
          <UserOutlined style={{ fontSize: 48, color: '#D9D9D9' }} />
          <div style={{ marginTop: 16, color: '#8C8C8C' }}>您没有访问权限</div>
        </Card>
      </div>
    )
  }

  const columns = [
    {
      title: '用户名',
      dataIndex: 'username',
      key: 'username',
      width: 120,
    },
    {
      title: '姓名',
      dataIndex: 'displayName',
      key: 'displayName',
      width: 120,
    },
    {
      title: '角色',
      dataIndex: 'roleValue',
      key: 'role',
      width: 120,
      render: (role: UserRole) => (
        <Tag color={roleColors[role]}>
          {UserRoleLabels[role]}
        </Tag>
      ),
    },
    {
      title: '部门',
      dataIndex: 'department',
      key: 'department',
      width: 120,
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm'),
    },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 24 }}>用户管理</Title>

      <Card bordered={false} style={{ borderRadius: 8 }}>
        {loading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
            <Spin size="large" />
          </div>
        ) : (
          <Table
            dataSource={users}
            columns={columns}
            rowKey="id"
            pagination={false}
          />
        )}
      </Card>

      <Card 
        bordered={false} 
        style={{ borderRadius: 8, marginTop: 16 }}
        size="small"
      >
        <div style={{ color: '#8C8C8C', fontSize: 13 }}>
          <div style={{ marginBottom: 8, color: '#595959', fontWeight: 500 }}>角色权限说明：</div>
          <div>• <Tag color="default">普通员工</Tag> - 可创建和管理自己的采购申请</div>
          <div>• <Tag color="blue">部门经理</Tag> - 可审批金额≤5000元的申请（第1级审批）</div>
          <div>• <Tag color="green">财务总监</Tag> - 可审批金额≤20000元的申请（第2级审批）</div>
          <div>• <Tag color="purple">总经理</Tag> - 可审批所有金额的申请（第3级审批）</div>
          <div>• <Tag color="red">系统管理员</Tag> - 可管理系统用户</div>
        </div>
      </Card>
    </div>
  )
}
