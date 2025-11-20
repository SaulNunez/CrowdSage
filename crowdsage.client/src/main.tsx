import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { BrowserRouter, Route, Routes } from 'react-router'
import QuestionPage from './Screens/QuestionPage.tsx'
import Login from './Screens/LoginPage.tsx'
import Register from './Screens/RegisterPage.tsx'
import { Provider } from 'react-redux'
import { store } from './store.ts'


createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <BrowserRouter>
      <Provider store={store}>
        <Routes>
          <Route index element={<App />} />
          <Route path="question">
            <Route path=":questionId" element={<QuestionPage/>} />
          </Route>
          <Route path="auth">
            <Route path="login" element={<Login/>} />
            <Route path="register" element={<Register />} />
          </Route>
        </Routes>
      </Provider>
      </BrowserRouter>
  </StrictMode>,
)
