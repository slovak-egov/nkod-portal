import axios, { AxiosError, AxiosResponse } from 'axios';
import React, { useCallback, useContext, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Dataset, useUserInfo } from './client';
import { sortComments } from './helpers/helpers';

const baseUrl = process.env.REACT_APP_CMS_API_URL ?? process.env.REACT_APP_API_URL + 'cms/';

export type AutocompleteOption<T> = {
    value: T;
    label: string;
};

export type NewCmsUser = {
    firstName: string;
    lastName: string;
    userName: string;
    email: string | undefined;
    password: string;
    passwordConfirm: string;
};

export type CmsCodelist = {
    id: string;
    label: string;
    values: CmsCodelistValue[];
};

export type CmsCodelistValue = {
    id: string;
    label: string;
};

export type CmsUserModel = {
    user: CmsUser;
    roles: CmsRole[];
    selectedRoles: string[] | null;
    password: string;
    passwordConfirm: string;
};

export type LoginPayload = {
    username: string;
    password: string;
};

export type AppRegistrationFormValues = {
    title: string;
    userId?: string;
    description: string;
    type: ApplicationType;
    theme: ApplicationTheme;
    url?: string | null;
    logo?: string;
    logoFiles: FileList | null;
    datasetURIsForm: { value: string }[];
    contactName: string;
    contactSurname: string;
    contactEmail: string;
};

export enum ApplicationType {
    MOBILE_APPLICATION = 'MA',
    WEB_APPLICATION = 'WA',
    WEB_PORTAL = 'WP',
    VISUALIZATION = 'V',
    ANALYSIS = 'A'
}

export interface RequestCmsQuery {
    searchQuery?: string;
    orderBy?: string;
    pageNumber: number;
    pageSize: number;
}

export interface RequestCmsSuggestionsQuery extends RequestCmsQuery {
    orgToUris?: string[] | null;
    types?: SuggestionType[] | null;
    statuses?: SuggestionStatusCode[] | null;
}

export interface RequestCmsApplicationsQuery extends RequestCmsQuery {
    types?: ApplicationType[] | null;
    themes?: ApplicationTheme[] | null;
}

export enum ApplicationTheme {
    EDUCATION = 'ED',
    HEALTH = 'HE',
    ENVIRONMENT = 'EN',
    TRANSPORT = 'TR',
    CULTURE = 'CU',
    TOURISM = 'TU',
    ECONOMY = 'EC',
    SOCIAL = 'SO',
    PUBLIC_ADMINISTRATION = 'PA',
    OTHER = 'O'
}

export enum SuggestionType {
    SUGGESTION_FOR_PUBLISHED_DATASET = 'PN',
    SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET = 'DQ',
    SUGGESTION_FOR_QUALITY_OF_METADATA = 'MQ',
    SUGGESTION_OTHER = 'O'
}

export enum SuggestionStatusCode {
    CREATED = 'C',
    IN_PROGRESS = 'P',
    RESOLVED = 'R'
}

export interface IComment {
    id: string;
    contentId: string;
    userId: string;
    author: string;
    parentId: string;
    email: string;
    body: string;
    created: string;
}

export interface ICommentSorted extends IComment {
    children: ICommentSorted[];
    depth: number;
}

export interface CommentFormValues {
    body: string;
}

export interface SuggestionFormValues {
    userId?: string;
    userOrgUri?: string | null;
    orgToUri?: any;
    type: string;
    datasetUri: any;
    title: string;
    description: string;
    status: string;
}

export enum SuggestionOrganizationCode {
    MH = 'MH',
    MF = 'MF',
    MD = 'MD',
    MPRV = 'MPRV',
    MV = 'MV',
    MO = 'MO',
    MS = 'MS',
    MZVEZ = 'MZVEZ',
    MPSR = 'MPSR',
    MZP = 'MZP',
    MSVVS = 'MSVVS',
    MK = 'MK',
    MZ = 'MZ',
    MIRRI = 'MIRRI',
    MCRAS = 'MCRAS'
}

export type OrganizationItem = {
    id: string;
    key: string;
    name: string;
    isPublic: boolean;
    datasetCount: number;
    themes?: {
        [key: string]: number | undefined;
    };
    nameAll: {
        sk: string;
    };
    website: any;
    email: any;
    phone: any;
    legalForm: any;
};

export type OrganizationList = {
    items: OrganizationItem[];
    facets: [];
    totalCount: number;
};

export type DatasetList = {
    items: Dataset[];
    facets: [];
    totalCount: number;
};

export interface EditSuggestionFormValues extends SuggestionFormValues {
    id: string;
    suggestionStatus: string;
}

export interface Audited {
    created: string;
    updated: string;
}

export interface Likeable {
    likeCount: number;
}

export interface Commentable {
    commentCount: number;
}

export interface Pageable<T> {
    items: T[];
    paginationMetadata: {
        totalItemCount: number;
        pageSize: number;
        currentPage: number;
    };
}

export interface Application extends Audited, Likeable, Commentable {
    id: string;
    title: string;
    userId?: string;
    description: string;
    type: ApplicationType;
    theme: ApplicationTheme;
    url: string;
    logo: string;
    logoFileName: string;
    datasetURIs: string[];
    contactName: string;
    contactSurname: string;
    contactEmail: string;
}

export interface SuggestionDetail extends Suggestion {
    orgName?: string;
    datasetName?: string;
}

export interface CmsDataset extends Likeable, Commentable {
    id: string;
    datasetUri: string;
    created: string;
    updated: string;
}

export interface Suggestion extends SuggestionFormValues, Audited, Likeable, Commentable {
    id: string;
    suggestionStatus: string;
    createdDate: Date;
    createdBy?: string;
}

export interface CmsBaseModel {
    concurrencyStamp: string | undefined;
}

export interface CmsUser extends CmsBaseModel {
    id: string;
    userName: string | undefined;
    normalizedUserName: string | undefined;
    email: string | undefined;
    normalizedEmail: string | undefined;
    emailConfirmed: boolean;
    passwordHash: string | undefined;
    securityStamp: string | undefined;
    phoneNumber: string | undefined;
    phoneNumberConfirmed: boolean;
    twoFactorEnabled: boolean;
    lockoutEnd: Date | undefined;
    lockoutEnabled: boolean;
    accessFailedCount: number;
}

export interface CmsRole extends CmsBaseModel {
    id: string;
    name: string | undefined;
    normalizedName: string | undefined;
    concurrencyStamp: string | undefined;
}

export interface CmsUserContextType {
    cmsUser: CmsUser | null;
    setCmsUser: (user: CmsUser | null) => void;
}

export const CmsUserContext = React.createContext<CmsUserContextType | null>(null);
const createUser = (request: NewCmsUser): CmsUserModel => ({
    user: {
        id: '00000000-0000-0000-0000-000000000000',
        userName: request.userName,
        normalizedUserName: undefined,
        email: request.email,
        normalizedEmail: undefined,
        emailConfirmed: false,
        passwordHash: undefined,
        securityStamp: undefined,
        concurrencyStamp: undefined,
        phoneNumber: undefined,
        phoneNumberConfirmed: false,
        twoFactorEnabled: false,
        lockoutEnd: undefined,
        lockoutEnabled: false,
        accessFailedCount: 0
    },
    roles: [],
    selectedRoles: ['SysAdmin'],
    password: request.password,
    passwordConfirm: request.passwordConfirm
});

export function useCmsUserAdd(initialValue: NewCmsUser) {
    return useEntityAdd<CmsUserModel>('manager/user/save', createUser(initialValue));
}

export function useCmsUserLogin(initialValue: LoginPayload) {
    return useEntityAdd<LoginPayload>('user/login', initialValue);
}

export function useCmsUserLogout() {
    return useEntityAddWithoutInput('user/logout');
}

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

export async function sendPost<TInput>(url: string, input: TInput) {
    return await axios.post(baseUrl + url, input);
}

export async function sendPut<TInput>(url: string, input: TInput) {
    return await axios.put(baseUrl + url, input);
}

export async function sendPostWithoutInput(url: string) {
    return await axios.post(baseUrl + url);
}

export async function sendGet(url: string) {
    return await axios.get(baseUrl + url);
}

export async function sendDelete(url: string) {
    return await axios.delete(baseUrl + url);
}

export async function getCmsUser(): Promise<CmsUser | null> {
    let response = await axios.get<CmsUser>(baseUrl + 'user/info');
    return response.status === 200 ? response.data : null;
}

export function useCmsUser() {
    return useContext(CmsUserContext)?.cmsUser ?? null;
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
    // const headers = useDefaultHeaders();

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

    const like = useCallback(
        async (url: string, contentId?: string, datasetUri?: string) => {
            setLoading(true);
            try {
                let response: AxiosResponse<Suggestion[]>;
                if (!contentId && datasetUri) {
                    response = await sendPost(url, {
                        datasetUri,
                        userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b1aa87'
                    });
                } else {
                    response = await sendPost(url, {
                        contentId,
                        datasetUri,
                        userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b1aa87'
                    });
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
        [userInfo?.id]
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
