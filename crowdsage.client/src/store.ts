import { setupListeners } from "@reduxjs/toolkit/query/react";
import { questionsApi } from "./store/reducers";
import { configureStore } from "@reduxjs/toolkit";
import authReducer from "./store/authSlice";

export const store = configureStore({
    reducer: {
        [questionsApi.reducerPath]: questionsApi.reducer,
        auth: authReducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(questionsApi.middleware),
});

setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export default store;