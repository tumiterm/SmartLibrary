import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Toaster } from 'sonner'
import { AppShell } from '@/components/layout/app-shell'
import { ThemeProvider, useTheme } from '@/components/theme-provider'
import { AddBookPage } from '@/pages/add-book-page'
import { BookDetailsPage } from '@/pages/book-details-page'
import { CatalogPage } from '@/pages/catalog-page'
import { CirculationPage } from '@/pages/circulation-page'
import { DashboardPage } from '@/pages/dashboard-page'
import { MemberDetailsPage } from '@/pages/member-details-page'
import { MembersPage } from '@/pages/members-page'
import { RegisterMemberPage } from '@/pages/register-member-page'
import { OpacBookPage } from '@/pages/opac/opac-book-page'
import { OpacHomePage } from '@/pages/opac/opac-home-page'
import { OpacShell } from '@/pages/opac/opac-shell'
import { ReportsPage } from '@/pages/reports-page'
import { SettingsPage } from '@/pages/settings-page'
import { StocktakePage } from '@/pages/stocktake-page'

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
            <Route path="opac" element={<OpacShell />}>
              <Route index element={<OpacHomePage />} />
              <Route path="books/:id" element={<OpacBookPage />} />
            </Route>
            <Route element={<AppShell />}>
              <Route index element={<DashboardPage />} />
              <Route path="catalog" element={<CatalogPage />} />
              <Route path="catalog/add" element={<AddBookPage />} />
              <Route path="catalog/books/:id" element={<BookDetailsPage />} />
              <Route path="circulation" element={<CirculationPage />} />
              <Route path="patrons" element={<MembersPage />} />
              <Route path="patrons/register" element={<RegisterMemberPage />} />
              <Route path="patrons/:id" element={<MemberDetailsPage />} />
              <Route path="stocktake" element={<StocktakePage />} />
              <Route path="reports" element={<ReportsPage />} />
              <Route path="settings" element={<SettingsPage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </BrowserRouter>
        <ThemedToaster />
      </QueryClientProvider>
    </ThemeProvider>
  )
}
