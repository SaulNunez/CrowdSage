import { setupListeners } from "@reduxjs/toolkit/query/react";
import { questionsApi } from "./common/reducers";
import { configureStore } from "@reduxjs/toolkit";

export const store = configureStore({
    reducer: {
        [questionsApi.reducerPath]: questionsApi.reducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(questionsApi.middleware),
});

setupListeners(store.dispatch);