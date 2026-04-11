export interface Draft {
  id: string;
  chapterId: string;
  title: string;
  content: string;
  wordCount: number;
  isPublished: boolean;
  publishMessage?: string;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDraftRequest {
  title: string;
  content?: string;
}

export interface UpdateDraftRequest {
  title?: string;
  content?: string;
}

export interface PublishDraftRequest {
  publishMessage: string;
}
