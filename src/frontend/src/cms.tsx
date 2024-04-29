import axios, { AxiosError, AxiosResponse, RawAxiosRequestHeaders } from 'axios';
import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useDefaultHeaders, useUserInfo } from './client';
import { AutocompleteOption } from './components/ReactSelectElement';
import { sortComments } from './helpers/helpers';
import {
    Application,
    Audited,
    CmsDataset,
    DatasetList,
    ICommentSorted,
    OrganizationList,
    Pageable,
    RequestCmsApplicationsQuery,
    RequestCmsQuery,
    RequestCmsSuggestionsQuery,
    Suggestion,
    SuggestionDetail
} from './interface/cms.interface';

const baseUrl = process.env.REACT_APP_CMS_API_URL ?? process.env.REACT_APP_API_URL + 'cms/';

export function useEntityAdd<T>(url: string, initialValue: T) {
    const [entity, setEntity] = useState<T>(initialValue);
    const [genericError, setGenericError] = useState<Error | null>(null);
    const [saving, setSaving] = useState(false);

    const save = useCallback(async () => {
        setSaving(true);
        setGenericError(null);
        try {
            const response: AxiosResponse<any> = await sendPost(url, entity);
            return { success: true, data: response };
        } catch (err) {
            if (err instanceof AxiosError) {
                if (err.response?.data?.status?.type === 'danger') {
                    setGenericError({
                        name: err.response.data.status.body?.replace(/(<([^>]+)>)/gi, '') ?? 'Error',
                        message: ''
                    });
                }
            } else if (err instanceof Error) {
                setGenericError(err);
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url]);

    const setEntityProperties = useCallback(
        (properties: Partial<T>) => {
            setEntity({ ...entity, ...properties });
        },
        [entity, setEntity]
    );

    return [entity, setEntityProperties, genericError, saving, save] as const;
}

export function useEntityAddWithoutInput(url: string) {
    const [genericError, setGenericError] = useState<Error | null>(null);
    const [saving, setSaving] = useState(false);

    const save = useCallback(async () => {
        setSaving(true);
        setGenericError(null);
        try {
            const response: AxiosResponse<any> = await sendPostWithoutInput(url);
            return { success: true, data: response };
        } catch (err) {
            if (err instanceof AxiosError) {
                if (err.response?.data?.status?.type === 'danger') {
                    setGenericError({
                        name: err.response.data.status.body?.replace(/(<([^>]+)>)/gi, '') ?? 'Error',
                        message: ''
                    });
                }
            } else if (err instanceof Error) {
                setGenericError(err);
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [url]);

    return [genericError, saving, save] as const;
}

export async function sendPost<TInput>(url: string, input: TInput, headers?: RawAxiosRequestHeaders) {
    return await axios.post(baseUrl + url, input, { headers });
}

export async function sendPut<TInput>(url: string, input: TInput, headers?: RawAxiosRequestHeaders) {
    return await axios.put(baseUrl + url, input, { headers });
}

export async function sendPostWithoutInput(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.post(baseUrl + url, null, { headers });
}

export async function sendGet(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.get(baseUrl + url, { headers });
}

export async function sendDelete(url: string, headers?: RawAxiosRequestHeaders) {
    return await axios.delete(baseUrl + url, { headers });
}

export function useCmsPublisherLists({
    language,
    page = 1,
    pageSize = 10000,
    orderBy = 'name'
}: {
    language: string;
    page?: number;
    pageSize?: number;
    orderBy?: string;
}) {
    const [publishers, setPublishers] = useState<AutocompleteOption<any>[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setPublishers([]);
            try {
                const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                    language: language,
                    page: page,
                    pageSize: pageSize,
                    orderBy: orderBy
                });
                if (response.data.items.length > 0) {
                    setPublishers(
                        response.data.items
                            .map((item) => ({
                                value: item,
                                label: item.nameAll.sk
                            }))
                            .sort((a, b) => a.label.localeCompare(b.label))
                    );
                }
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
            } finally {
                setLoading(false);
            }
        }

        load();
    }, [language, page, pageSize, orderBy]);

    return [publishers, loading, error] as const;
}

export function useCmsApplications(autoload: boolean = true, datasetUri?: string) {
    const [items, setItems] = useState<Application[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async (datasetUri?: string) => {
        setLoading(true);
        setItems([]);
        try {
            const response: AxiosResponse<Pageable<Application>> = await sendGet(`cms/applications${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
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
                const response: AxiosResponse<Application> = await sendGet(`cms/applications/${id}`);

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
            const response: AxiosResponse<T> = await sendPost(url, query);
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
                    response = await sendPost(
                        url,
                        {
                            datasetUri,
                            userId: userInfo?.id
                        },
                        headers
                    );
                } else {
                    response = await sendPost(
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
                const response: AxiosResponse<Pageable<ICommentSorted>> = await sendGet(`cms/comments?contentId=${contentId}`);
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
                const response: AxiosResponse<Suggestion> = await sendGet(`cms/suggestions/${id}`);
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
            const response: AxiosResponse<Pageable<CmsDataset>> = await sendGet(`cms/datasets${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
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
            const response: AxiosResponse<Pageable<Suggestion>> = await sendGet(`cms/suggestions${datasetUri ? `?datasetUri=${datasetUri}` : ''}`);
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

export const useCmsSearchPublisherLists = ({
    language,
    page = 1,
    pageSize = 50,
    orderBy = 'name',
    queryText
}: {
    language: 'sk' | 'en';
    page?: number;
    pageSize?: number;
    orderBy?: string;
    queryText: string;
}) => {
    const [publishers, setPublishers] = useState<AutocompleteOption<string>[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const load = async (query: string) => {
        setLoading(true);
        setError(null);
        setPublishers([]);
        try {
            const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                language: language,
                page: page,
                pageSize: pageSize,
                orderBy: orderBy,
                queryText: queryText
            });
            if (response.data.items.length > 0) {
                setPublishers(
                    response.data.items
                        .map((item) => ({
                            value: item.id,
                            label: item.nameAll.sk
                        }))
                        .sort((a, b) => a.label.localeCompare(b.label))
                );
            } else {
                setPublishers([]);
            }
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load(queryText);
    }, [language, page, pageSize, orderBy, queryText]);

    return [publishers, loading, error] as const;
};

export const useSearchPublisher = ({ language, query }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [publishers, setPublishers] = useState<AutocompleteOption<string>[]>([]);

    const load = async (query: string, filters?: any, pageSize = 50) => {
        setLoading(true);
        setError(null);
        try {
            const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                language: language,
                page: 1,
                filters,
                pageSize,
                orderBy: 'name',
                queryText: query
            });
            let data: AutocompleteOption<any>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item) => ({
                        value: item.key,
                        label: item.nameAll.sk
                    }))
                    .sort((a, b) => a.label.localeCompare(b.label));
            }
            setPublishers(data);
            return data as AutocompleteOption<string>[];
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load(query);
    }, [query]);

    return [publishers, loading, error, load] as const;
};

export const useSearchDataset = ({ language, query, filters }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [datasets, setDatasets] = useState<AutocompleteOption<string>[]>([]);

    const load = async (query: string, filters?: any, pageSize = 50) => {
        setLoading(true);
        setError(null);
        try {
            const response = await axios.post<DatasetList>('https://wpnkod.informo.sk/datasets/search', {
                language,
                page: 1,
                filters,
                pageSize,
                orderBy: 'name',
                queryText: query
            });
            let data: AutocompleteOption<string>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item) => ({
                        value: item.key,
                        label: item.name ?? ''
                    }))
                    .sort((a, b) => a.label.localeCompare(b.label));
            }
            setDatasets(data);
            return data as AutocompleteOption<string>[];
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (filters) {
            load(query);
        }
    }, [query]);

    return [datasets, loading, error, load] as const;
};
