import axios, { AxiosResponse, RawAxiosRequestHeaders } from 'axios';
import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useDefaultHeaders, useSearchDataset, useSearchPublisher, useUserInfo } from './client';
import { sortComments } from './helpers/helpers';
import {
    Application,
    Audited,
    CmsDataset,
    ICommentSorted,
    Pageable,
    RequestCmsApplicationsQuery,
    RequestCmsQuery,
    RequestCmsSuggestionsQuery,
    Suggestion,
    SuggestionDetail
} from './interface/cms.interface';

const cmsUrl = process.env.REACT_APP_CMS_API_URL;

export async function sendCmsPost<TInput>(url: string, input: TInput, headers?: RawAxiosRequestHeaders) {
    return await axios.post(cmsUrl + url, input, { headers });
}

export async function sendCmsPut<TInput>(url: string, input: TInput, headers?: RawAxiosRequestHeaders) {
    return await axios.put(cmsUrl + url, input, { headers });
}

export async function sendCmsPostWithoutInput(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.post(cmsUrl + url, null, { headers });
}

export async function sendCmsGet(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.get(cmsUrl + url, { headers });
}

export async function sendCmsDelete(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.delete(cmsUrl + url, { headers });
}

export function useCmsApplications(autoload: boolean = true, datasetUri?: string) {
    const [items, setItems] = useState<Application[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async (datasetUri?: string) => {
        setLoading(true);
        setItems([]);
        try {
            const response: AxiosResponse<Pageable<Application>> = await sendCmsGet(`applications${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
            setItems(response.data?.items?.sort(sortByUpdatedCreated) ?? []);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        if (autoload) {
            refresh(datasetUri);
        }
    }, [refresh, datasetUri, autoload]);

    return [items, loading, error, refresh] as const;
}

export function useCmsApplication(id?: string) {
    const [application, setApplication] = useState<Application | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async () => {
        setLoading(true);
        if (id) {
            try {
                const response: AxiosResponse<Application> = await sendCmsGet(`applications/${id}`);

                setApplication(response.data ?? null);
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
                setApplication(null);
            } finally {
                setLoading(false);
            }
        }
    }, []);

    useEffect(() => {
        refresh();
    }, [refresh]);

    return [application, loading, error, refresh] as const;
}

export function useCmsApplicationsSearch(initialQuery?: Partial<RequestCmsApplicationsQuery>) {
    return useCmsEntities<Pageable<Application>>('applications/search', { orderBy: 'modified', ...initialQuery });
}

export function useCmsSuggestionsSearch(initialQuery?: Partial<RequestCmsSuggestionsQuery>) {
    return useCmsEntities<Pageable<Suggestion>>('suggestions/search', { orderBy: 'modified', ...initialQuery });
}

export function useCmsEntities<T>(url: string, initialQuery?: any) {
    const [searchParams] = useSearchParams();

    let defaultParams: RequestCmsQuery = {
        pageSize: 10,
        pageNumber: 0,
        searchQuery: '',
        orderBy: 'name'
    };

    if (searchParams.has('query')) {
        defaultParams = {
            ...defaultParams,
            searchQuery: searchParams.get('query')!
        };
    }

    const [query, setQuery] = useState<RequestCmsQuery>({
        ...defaultParams,
        ...initialQuery
    });
    const [items, setItems] = useState<T | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async () => {
        setLoading(true);
        if (error !== null) {
            setError(null);
        }
        try {
            const response: AxiosResponse<T> = await sendCmsPost(url, query);
            setItems(response.data);
            setLoading(false);
        } catch (err) {
            if (axios.isCancel(err)) {
                return;
            }
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
            setLoading(false);
        }
    }, [query, url]);

    useEffect(() => {
        refresh();
    }, [refresh]);

    const setQueryParameters = useCallback((query: Partial<RequestCmsQuery>) => {
        setQuery((q) => ({ ...q, ...query }));
    }, []);

    return [items, query, setQueryParameters, loading, error, refresh] as const;
}

export function useCmsLike() {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();

    const like = useCallback(
        async (url: string, contentId?: string, datasetUri?: string) => {
            setLoading(true);
            try {
                let response: AxiosResponse<Suggestion[]>;
                if (!contentId && datasetUri) {
                    response = await sendCmsPost(
                        url,
                        {
                            datasetUri,
                            userId: userInfo?.id
                        },
                        headers
                    );
                } else {
                    response = await sendCmsPost(
                        url,
                        {
                            contentId,
                            datasetUri,
                            userId: userInfo?.id
                        },
                        headers
                    );
                }
                setLoading(false);
                return response.status === 200;
            } catch (err) {
                setLoading(false);
                if (err instanceof Error) {
                    setError(err);
                    console.error('Like error', err.message);
                }
                return false;
            }
        },
        [userInfo?.id, headers]
    );

    return [loading, error, like] as const;
}

export function useCmsComments(contentId?: string) {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [comments, setComments] = useState<ICommentSorted[]>([]);

    const load = useCallback(async (contentId?: string) => {
        if (contentId) {
            setLoading(true);
            try {
                const response: AxiosResponse<Pageable<ICommentSorted>> = await sendCmsGet(`comments?contentId=${contentId}`);
                if (response.status === 200) {
                    setComments(sortComments(response.data?.items));
                }
                setLoading(false);
                return;
            } catch (err) {
                setLoading(false);
                if (err instanceof Error) {
                    setError(err);
                    console.error('Load comments error', err.message);
                }
                return false;
            }
        }
    }, []);

    useEffect(() => {
        load(contentId);
    }, [load, contentId]);

    return [comments, loading, error, load] as const;
}

export function useCmsSuggestion(id?: string) {
    const [suggestion, setSuggestion] = useState<SuggestionDetail | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const [publishers, loadingPublishers, errorPublishers, searchPublisher] = useSearchPublisher({
        language: 'sk',
        query: ''
    });

    const [datasets, loadingDatasetList, errorDatasetList, searchDataset] = useSearchDataset({
        language: 'sk',
        query: ''
    });

    const refresh = useCallback(async () => {
        setLoading(true);
        if (id) {
            try {
                const response: AxiosResponse<Suggestion> = await sendCmsGet(`suggestions/${id}`);
                const suggestionDetail: SuggestionDetail = { ...response.data } ?? null;

                const publisherItems = await searchPublisher('', { key: [suggestionDetail.orgToUri] }, 1);
                suggestionDetail.orgName = publisherItems?.[0]?.label;

                const datasetItems = await searchDataset('', { key: [suggestionDetail.datasetUri] }, 1);
                suggestionDetail.datasetName = datasetItems?.[0]?.label;

                setSuggestion(suggestionDetail ?? null);
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
                setSuggestion(null);
            } finally {
                setLoading(false);
            }
        }
    }, []);

    useEffect(() => {
        refresh();
    }, [refresh]);

    return [suggestion, loading, error, refresh] as const;
}

export function useCmsDatasets(autoload: boolean = true, datasetUri?: string) {
    const [datasets, setDatasets] = useState<CmsDataset[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async (datasetUri?: string) => {
        setDatasets([]);
        setLoading(true);
        try {
            const response: AxiosResponse<Pageable<CmsDataset>> = await sendCmsGet(`datasets${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
            setDatasets(response.data?.items ?? []);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setDatasets(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        if (autoload) {
            refresh(datasetUri);
        }
    }, [refresh, datasetUri, autoload]);

    return [datasets, loading, error, refresh] as const;
}

export function useCmsSuggestions(autoload: boolean = true, datasetUri?: string) {
    const [items, setItems] = useState<Suggestion[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async (datasetUri?: string) => {
        setItems([]);
        setLoading(true);
        try {
            const response: AxiosResponse<Pageable<Suggestion>> = await sendCmsGet(`suggestions${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
            setItems(response.data?.items.sort(sortByUpdatedCreated) ?? []);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        if (autoload) {
            refresh(datasetUri);
        }
    }, [refresh, datasetUri, autoload]);

    return [items, loading, error, refresh] as const;
}

const sortByUpdatedCreated = (a: Audited, b: Audited) => {
    const getDate = (dateStr: string) => (dateStr ? new Date(dateStr).getTime() : 0);
    const aUpdated = getDate(a.updated);
    const bUpdated = getDate(b.updated);
    const aCreated = new Date(a.created).getTime();
    const bCreated = new Date(b.created).getTime();

    return bUpdated - aUpdated || aCreated - bCreated;
};
