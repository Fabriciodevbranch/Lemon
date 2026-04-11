export interface Chapter {
  id: string;
  bookId: string;
  title: string;
  order: number;
  content?: string;
  wordCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateChapterRequest {
  title: string;
  order?: number;
}

export interface UpdateChapterRequest {
  title?: string;
  content?: string;
  order?: number;
}
