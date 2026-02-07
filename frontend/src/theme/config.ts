import type { ThemeConfig } from 'antd'

export const themeConfig: ThemeConfig = {
  token: {
    // 品牌色
    colorPrimary: '#1677FF',
    
    // 圆角
    borderRadius: 6,
    borderRadiusLG: 8,
    
    // 字体
    fontSize: 14,
    
    // 阴影
    boxShadow: '0 1px 2px 0 rgba(0, 0, 0, 0.03), 0 1px 6px -1px rgba(0, 0, 0, 0.02), 0 2px 4px 0 rgba(0, 0, 0, 0.02)',
  },
  components: {
    Layout: {
      siderBg: '#001529',
      headerBg: '#FFFFFF',
      bodyBg: '#F0F2F5',
    },
    Menu: {
      darkItemBg: '#001529',
      darkItemSelectedBg: '#1677FF',
    },
    Card: {
      paddingLG: 24,
    },
  },
}
