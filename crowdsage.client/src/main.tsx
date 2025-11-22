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
import CreateQuestionPage from './Screens/CreateQuestionPage.tsx'
import NavBar from './Components/NavBar.tsx'
import BookmarksScreen from './Screens/BookmarksPage.tsx'


createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Provider store={store}>
      <BrowserRouter>
        <NavBar isAuthenticated={false} />
        <Routes>
          <Route index element={<App />} />
          <Route path="question">
            <Route path=":questionId" element={<QuestionPage />} />
            <Route path="new" element={<CreateQuestionPage />} />
          </Route>
          <Route path="profile">
            <Route path="bookmarks" element={<BookmarksScreen />} />
          </Route>
          <Route path="auth">
            <Route path="login" element={<Login />} />
            <Route path="register" element={<Register />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </Provider>
  </StrictMode>,
)
