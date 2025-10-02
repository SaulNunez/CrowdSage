import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { createBrowserRouter, RouterProvider } from 'react-router';
import QuestionPage from './Screens/QuestionPage.tsx';

const router = createBrowserRouter([
  {
    path: "/",
    Component: App,
    children: [
      {
        path: "question/:questionId",
        Component: QuestionPage
      }
    ]
  },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
