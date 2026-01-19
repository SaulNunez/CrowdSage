import { setupListeners } from "@reduxjs/toolkit/query/react";
import { questionsApi } from "./common/reducers";
import { configureStore } from "@reduxjs/toolkit";
import authReducer from "./common/authSlice";

export const store = configureStore({
    reducer: {
        [questionsApi.reducerPath]: questionsApi.reducer,
        auth: authReducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(questionsApi.middleware),
});

setupListeners(store.dispatch);