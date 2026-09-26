import { useCallback } from "react";
import { useUploadMediaMutation } from "../store/reducers";

// Uploads an image to the server and resolves with the URL to embed in markdown.
// Pass the result to MarkdownEditor's `uploadImage` prop.
export function useImageUpload() {
  const [uploadMedia] = useUploadMediaMutation();
  return useCallback(
    async (file: File) => {
      const media = await uploadMedia(file).unwrap();
      return media.url;
    },
    [uploadMedia]
  );
}
