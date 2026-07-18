import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AppShell } from '@/components/layout/app-shell'
import { ThemeProvider, useTheme } from '@/components/theme-provider'
import { AddBookPage } from '@/pages/add-book-page'
import { DashboardPage } from '@/pages/dashboard-page'

const queryClient = new QueryClient()

function ThemedToaster() {
  const { theme } = useTheme()
  return <Toaster theme={theme} position="bottom-right" richColors closeButton />
}

export default function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route element={<AppShell />}>
              <Route index element={<DashboardPage />} />
              <Route path="catalog/add" element={<AddBookPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </BrowserRouter>
        <ThemedToaster />
      </QueryClientProvider>
    </ThemeProvider>
  )
}
