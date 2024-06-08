import axios, { AxiosError, AxiosRequestConfig, AxiosResponse, RawAxiosRequestHeaders } from 'axios';
import React, { useCallback, useContext, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useSearchParams } from 'react-router-dom';
import { AutocompleteOption, MORE_FAKE_OPTION } from './components/ReactSelectElement';

const baseUrl = process.env.REACT_APP_API_URL;

export type OrderOption = {
    name: string;
    value: string;
};

export enum LoginMethod {
    EGOV = '',
    GOOGLE = 'google'
}

export type TokenContextType = {
    token: TokenResult | null;
    setToken: (token: TokenResult | null) => void;
    userInfo: UserInfo | null;
    userInfoLoading: boolean;
    defaultHeaders: RawAxiosRequestHeaders;
};
export const TokenContext = React.createContext<TokenContextType | null>(null);

export type LanguageOptionsContextType = {
    language: Language;
    setLanguage: (language: Language) => void;
};
export const LanguageOptionsContext = React.createContext<LanguageOptionsContextType | null>(null);

export type TokenResult = {
    token: string;
    expires: string | null;
    refreshTokenAfter: string | null;
    refreshTokenInSeconds: number;
    refreshToken: string | null;
    redirectUrl: string | null;
};

export const knownCodelists = {
    dataset: {
        theme: 'http://publications.europa.eu/resource/authority/data-theme',
        type: 'https://data.gov.sk/set/codelist/dataset-type',
        accrualPeriodicity: 'http://publications.europa.eu/resource/authority/frequency',
        spatial: 'https://data.gov.sk/def/ontology/location',
        euroVoc: 'http://eurovoc.europa.eu/100141'
    },
    distribution: {
        personalDataContainmentType: 'https://data.gov.sk/set/codelist/personal-data-occurence-type',
        license: 'http://publications.europa.eu/resource/authority/licence',
        format: 'http://publications.europa.eu/resource/authority/file-type',
        mediaType: 'http://www.iana.org/assignments/media-types'
    },
    catalog: {
        type: 'https://data.gov.sk/def/local-catalog-type'
    },
    publisher: {
        legalForm: 'https://data.gov.sk/set/codelist/CL000056'
    }
};

export type Publisher = {
    id: string;
    key: string;
    isPublic: boolean;
    name: string;
    datasetCount: number;
    themes: { [id: string]: number } | null;
    nameAll: LanguageDependentTexts | null;
    website: string | null;
    email: string | null;
    phone: string | null;
    legalForm: string | null;
};

export type PublisherInput = {
    website: string;
    email: string;
    phone: string;
    legalForm: string;
};

export type AdminPublisherInput = {
    id: string | null;
    name: LanguageDependentTexts;
    uri: string;
    isEnabled: boolean;
} & PublisherInput;

type Temporal = {
    startDate: string | null;
    endDate: string | null;
};

type CardView = {
    name: string | null;
    nameAll: LanguageDependentTexts | null;
    email: string | null;
};

export type SaveResult = {
    id: string;
    success: boolean;
    errors: { [id: string]: string };
};

export type UserSaveResult = {
    invitationToken: string | null;
} & SaveResult;

export type CodelistValue = {
    id: string;
    label: string;
};

export type LanguageDependentTexts = {
    [id: string]: string;
};

export type LanguageDependentTextsMulti = {
    [id: string]: string[];
};

export type Language = {
    id: string;
    name: string;
    nameInPrimaryLanguage: string;
    isPrimary: boolean;
    isRequired: boolean;
};

export type UserInfo = {
    publisher: string | null;
    publisherView: Publisher;
    publisherEmail: string | null;
    publisherHomePage: string | null;
    publisherPhone: string | null;
    publisherActive: boolean;
    publisherLegalForm: string | null;
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: string | null;
    companyName: string;
};

export type DatasetInput = {
    id?: string;
    isPublic: boolean;
    name: LanguageDependentTexts;
    description: LanguageDependentTexts;
    themes: string[];
    accrualPeriodicity: string | null;
    keywords: LanguageDependentTextsMulti;
    type: string[];
    spatial: string[];
    startDate: string | null;
    endDate: string | null;
    contactName: LanguageDependentTexts;
    contactEmail: string | null;
    landingPage: string | null;
    specification: string | null;
    euroVocThemes: string[];
    spatialResolutionInMeters: string | null;
    temporalResolution: string | null;
    isPartOf: string | null;
    isSerie: boolean;
};

export type DistributionInput = {
    id?: string;
    datasetId: string | null;
    authorsWorkType: string | null;
    originalDatabaseType: string | null;
    databaseProtectedBySpecialRightsType: string | null;
    personalDataContainmentType: string | null;
    authorName: string | null;
    originalDatabaseAuthorName: string | null;
    downloadUrl: string | null;
    format: string | null;
    mediaType: string | null;
    conformsTo: string | null;
    compressFormat: string | null;
    packageFormat: string | null;
    title: LanguageDependentTexts | null;
    fileId: string | null;
};

export type LocalCatalogInput = {
    id?: string;
    isPublic: boolean;
    name: LanguageDependentTexts;
    description: LanguageDependentTexts;
    contactName: LanguageDependentTexts;
    contactEmail: string | null;
    homePage: string | null;
    type: string | null;
    endpointUrl: string | null;
};

export type Dataset = {
    id: string;
    key: string;
    isPublic: boolean;
    name: string | null;
    nameAll: LanguageDependentTexts | null;
    description: string | null;
    descriptionAll: LanguageDependentTexts | null;
    publisherId: string | null;
    publisher: Publisher | null;
    themes: string[];
    themeValues: CodelistValue[];
    accrualPeriodicity: string | null;
    accrualPeriodicityValue: CodelistValue | null;
    keywords: string[];
    keywordsAll: LanguageDependentTextsMulti | null;
    type: string[];
    typeValues: CodelistValue[];
    spatial: string[];
    spatialValues: CodelistValue[];
    temporal: Temporal | null;
    contactPoint: CardView | null;
    landingPage: string | null;
    specification: string | null;
    euroVocThemes: string[];
    euroVocThemeValues: string[];
    spatialResolutionInMeters: number | null;
    temporalResolution: string | null;
    isPartOf: string | null;
    isSerie: boolean;
    distributions: Distribution[];
    isHarvested: boolean;
    issued: string | null;
    lastUpdated: string | null;
};

type TermsOfUse = {
    authorsWorkType: string | null;
    originalDatabaseType: string | null;
    databaseProtectedBySpecialRightsType: string | null;
    personalDataContainmentType: string | null;
    authorsWorkTypeValue: CodelistValue | null;
    originalDatabaseTypeValue: CodelistValue | null;
    databaseProtectedBySpecialRightsTypeValue: CodelistValue | null;
    personalDataContainmentTypeValue: CodelistValue | null;
    authorName: string | null;
    originalDatabaseAuthorName: string | null;
};

export type Distribution = {
    id: string;
    datasetId: string | null;
    termsOfUse: TermsOfUse | null;
    downloadUrl: string | null;
    accessUrl: string | null;
    format: string | null;
    formatValue: CodelistValue | null;
    mediaType: string | null;
    mediaTypeValue: CodelistValue | null;
    conformsTo: string | null;
    compressFormat: string | null;
    compressFormatValue: CodelistValue | null;
    packageFormat: string | null;
    packageFormatValue: CodelistValue | null;
    title: string | null;
    titleAll: LanguageDependentTexts | null;
    isHarvested: boolean;
};

export type LocalCatalog = {
    id: string;
    isPublic: boolean;
    name: string;
    nameAll: LanguageDependentTexts | null;
    description: string | null;
    descriptionAll: LanguageDependentTexts | null;
    publisher: Publisher | null;
    contactPoint: CardView | null;
    homePage: string | null;
    type: string | null;
    typeValue: CodelistValue | null;
    endpointUrl: string | null;
};

export type Codelist = {
    id: string;
    label: string;
    values: CodelistValue[];
};

export type Facet = {
    id: string;
    values: { [id: string]: number };
};

export type Response<T> = {
    items: T[];
    totalCount: number;
    facets: Facet[];
};

export type RequestQuery = {
    pageSize: number;
    page: number;
    queryText?: string;
    language: string;
    orderBy?: string;
    filters?: { [id: string]: string[] };
    requiredFacets: string[];
};

type User = {
    id: string;
    firstName: string;
    lastName: string;
    email: string | null;
    role: string | null;
    isActive: boolean;
    invitationExpiresAt: string | null;
};

export type UserActivation = {
    id: string | null;
    token: string | null;
};

export type UserForgottenPasswordForm = {
    email: string;
};

export type UserForgottenPasswordActivationForm = {
    id: string | null;
    token: string | null;
    password: string;
    passwordConfirm: string;
};

export type UserLoginForm = {
    email: string;
    password: string;
};

export type UserRegistrationForm = {
    email: string;
    password: string;
    passwordConfirm: string;
    firstName: string;
    lastName: string;
};

type CodelistAdminView = {
    id: string;
    label: string;
    count: number;
};

export type NewUser = {
    firstName: string;
    lastName: string;
    email: string | null;
    role: string | null;
};

export type EditUser = {
    id: string;
    firstName: string;
    lastName: string;
    email: string | null;
    role: string | null;
};

export function useDelay<T>(initialValue: T, delay: number, callback: (value: T) => void) {
    const [value, setValue] = useState<T>(initialValue);

    useEffect(() => {
        const timeout = setTimeout(() => callback(value), delay);
        return () => clearTimeout(timeout);
    }, [value, callback, delay]);

    return [value, setValue] as const;
}

export function useEntities<T>(url: string, initialQuery?: Partial<RequestQuery>) {
    const [searchParams] = useSearchParams();

    const l = useContext(LanguageOptionsContext);
    const language = l?.language ?? supportedLanguages[0];

    let defaultParams: RequestQuery = {
        pageSize: 10,
        page: 1,
        queryText: '',
        language: language.id,
        orderBy: 'name',
        filters: {},
        requiredFacets: []
    };

    if (searchParams.has('query')) {
        defaultParams = {
            ...defaultParams,
            queryText: searchParams.get('query')!
        };
    }

    const [query, setQuery] = useState<RequestQuery>({
        ...defaultParams,
        ...initialQuery
    });
    const [items, setItems] = useState<Response<T> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();

    const refresh = useCallback(
        async (abortController: AbortController | null = null) => {
            setLoading(true);
            if (error !== null) {
                setError(null);
            }
            if (query.page > 0) {
                try {
                    const response: AxiosResponse<Response<T>> = await sendPost(url, query, headers, abortController);
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
            }
        },
        [query, url, headers]
    );

    useEffect(() => {
        const abortController = new AbortController();
        refresh(abortController);
        return () => {
            abortController.abort();
        };
    }, [refresh]);

    const setQueryParameters = useCallback((query: Partial<RequestQuery>) => {
        setQuery((q) => ({ ...q, ...query }));
    }, []);

    useEffect(() => {
        if (language.id !== query.language) {
            setQueryParameters({ language: language.id });
        }
    }, [language, setQueryParameters, query]);

    return [items, query, setQueryParameters, loading, error, refresh] as const;
}

export function useEntity<T>(url: string, sourceId?: string) {
    const { id } = useParams();
    const [item, setItem] = useState<T | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();

    const l = useContext(LanguageOptionsContext);
    const language = l?.language ?? supportedLanguages[0];

    const targetId = sourceId ?? id;

    useEffect(() => {
        async function load() {
            if (targetId) {
                const query: RequestQuery = {
                    language: language.id,
                    page: 1,
                    pageSize: 1,
                    filters: {
                        id: [targetId]
                    },
                    requiredFacets: []
                };

                setLoading(true);
                setError(null);
                setItem(null);
                try {
                    const response: AxiosResponse<Response<T>> = await sendPost(url, query, headers);
                    if (response.data.items.length > 0) {
                        setItem(response.data.items[0]);
                    }
                } catch (err) {
                    if (err instanceof Error) {
                        setError(err);
                    }
                } finally {
                    setLoading(false);
                }
            }
        }

        load();
    }, [targetId, url, headers, language]);

    return [item, loading, error] as const;
}

export function useDataset(id?: string) {
    return useEntity<Dataset>('datasets/search', id);
}

export function useLocalCatalog() {
    return useEntity<LocalCatalog>('local-catalogs/search');
}

export function useDatasets(initialQuery?: Partial<RequestQuery>) {
    let defaultParams: Partial<RequestQuery> = { ...initialQuery };

    const [searchParams] = useSearchParams();
    if (searchParams.has('publisher')) {
        defaultParams = {
            ...defaultParams,
            filters: {
                ...defaultParams.filters,
                publishers: [searchParams.get('publisher')!]
            }
        };
    }

    return useEntities<Dataset>('datasets/search', { orderBy: 'modified', ...defaultParams });
}

export function useLocalCatalogs(initialQuery?: Partial<RequestQuery>) {
    return useEntities<LocalCatalog>('local-catalogs/search', initialQuery);
}

export function usePublishers(initialQuery?: Partial<RequestQuery>) {
    return useEntities<Publisher>('publishers/search', { orderBy: 'relevance', ...initialQuery });
}

export function useDistributions(initialQuery?: Partial<RequestQuery>) {
    return useEntities<Distribution>('distributions/search', { orderBy: 'relevance', ...initialQuery });
}

export function useUsers() {
    type Query = {
        page: number;
        pageSize: number;
    };

    const [query, setQuery] = useState<Query>({
        page: 1,
        pageSize: 10
    });
    const [items, setItems] = useState<Response<User> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();

    const refresh = useCallback(async () => {
        setLoading(true);
        if (query.page > 0) {
            try {
                const response: AxiosResponse<Response<User>> = await sendPost('users/search', query, headers);
                setItems(response.data);
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
                setItems(null);
            } finally {
                setLoading(false);
            }
        }
    }, [query, headers]);

    useEffect(() => {
        refresh();
    }, [refresh]);

    const setQueryParameters = useCallback((query: Partial<Query>) => {
        setQuery((q) => ({ ...q, ...query }));
    }, []);

    return [items, query, setQueryParameters, loading, error, refresh] as const;
}

export function useUserLogin() {
    const [logging, setLogging] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();
    const { t } = useTranslation();

    const login = useCallback(
        async (request: UserLoginForm) => {
            setLogging(true);
            try {
                const response: AxiosResponse<TokenResult> = await sendPost('users/login', request, headers);
                if (!response?.data?.token) {
                    throw new Error(t('loginPage.failed'));
                }
                return { success: Boolean(response?.data?.token), data: response };
            } catch (err) {
                if (err instanceof AxiosError) {
                    if (err.response?.status === 403) {
                        setError(new Error(t('loginPage.incorrectLoginOrPassword')));
                    } else {
                        setError(err);
                    }
                }
                return { success: false };
            } finally {
                setLogging(false);
            }
        },
        [headers]
    );

    return [logging, error, login] as const;
}

export function useUserForgottenPassword() {
    const [sending, setSending] = useState(false);
    const [success, setSuccess] = useState(false);
    const [errors, setErrors] = useState<Error[] | null>(null);
    const headers = useDefaultHeaders();

    const sendEmail = useCallback(
        async (request: UserForgottenPasswordForm) => {
            setSending(true);
            try {
                const response: AxiosResponse<SaveResult> = await sendPost('users/recovery', request, headers);
                setSuccess(response.data?.success);
                if (!response?.data?.success) {
                    setErrors(concatErrors(response.data.errors));
                }
            } catch (err) {
                if (err instanceof Error) {
                    setErrors([err]);
                }
            } finally {
                setSending(false);
            }
        },
        [headers]
    );

    return [success, sending, errors, sendEmail] as const;
}

export function useUserForgottenActivationPassword() {
    const [sending, setSending] = useState(false);
    const [success, setSuccess] = useState(false);
    const [errors, setErrors] = useState<Error[] | null>(null);
    const headers = useDefaultHeaders();

    const changePassword = useCallback(
        async (request: UserForgottenPasswordActivationForm) => {
            setSending(true);
            try {
                const response: AxiosResponse<SaveResult> = await sendPost('users/recovery-activation', request, headers);
                setSuccess(response.data?.success);
                if (!response?.data?.success) {
                    setErrors(concatErrors(response.data.errors));
                }
            } catch (err) {
                if (err instanceof Error) {
                    setErrors([err]);
                }
            } finally {
                setSending(false);
            }
        },
        [headers]
    );

    return [success, sending, errors, changePassword] as const;
}

export function useUserRegister() {
    const [saving, setSaving] = useState(false);
    const [errors, setErrors] = useState<Error[] | null>(null);
    const headers = useDefaultHeaders();

    const register = useCallback(
        async (request: UserRegistrationForm) => {
            setSaving(true);
            try {
                const response: AxiosResponse<SaveResult> = await sendPost('users/register', request, headers);
                if (!response?.data?.success) {
                    setErrors(concatErrors(response.data.errors));
                }
                return { success: response?.data?.success, data: response };
            } catch (err) {
                if (err instanceof Error) {
                    setErrors([err]);
                }
                return { success: false };
            } finally {
                setSaving(false);
            }
        },
        [headers]
    );

    return [saving, errors, register] as const;
}

export function useUserActivate() {
    const [saving, setSaving] = useState(false);
    const [errors, setErrors] = useState<Error[] | null>(null);
    const headers = useDefaultHeaders();

    const activate = useCallback(
        async (request: UserActivation) => {
            setSaving(true);
            try {
                const response: AxiosResponse<SaveResult> = await sendPost('users/activation', request, headers);
                if (!response?.data?.success) {
                    setErrors(concatErrors(response.data.errors));
                }
            } catch (err) {
                if (err instanceof Error) {
                    setErrors([err]);
                }
            } finally {
                setSaving(false);
            }
        },
        [headers]
    );
    return [saving, errors, activate] as const;
}

export function useCodelists(keys: string[]) {
    const [codelists, setCodelists] = useState<Codelist[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setCodelists([]);
            try {
                const response: AxiosResponse<Codelist[]> = await axios.get(baseUrl + 'codelists', {
                    params: {
                        keys: keys
                    }
                });
                if (response.data.length > 0) {
                    setCodelists(response.data);
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
    }, [keys]);

    return [codelists, loading, error] as const;
}

export async function searchCodelistItem(codelistId: string, query: string) {
    const response: AxiosResponse<Codelist[]> = await axios.post(
        baseUrl + 'codelists/item',
        {},
        {
            params: {
                key: codelistId,
                query: query
            }
        }
    );
    return response.data;
}

export async function getCodelistItem(codelistId: string, id: string) {
    try {
        const response: AxiosResponse<CodelistValue> = await axios.get(baseUrl + 'codelists/item', {
            params: {
                key: codelistId,
                id: id
            }
        });
        return response.data;
    } catch (error) {
        if (error instanceof AxiosError) {
            if (error.response?.status === 404) {
                return null;
            }
        }
    }
    return null;
}

export function useEntityAdd<T>(url: string, initialValue: T) {
    const [entity, setEntity] = useState<T>(initialValue);
    const [errors, setErrors] = useState<{ [id: string]: string }>({});
    const [saving, setSaving] = useState(false);
    const headers = useDefaultHeaders();

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<SaveResult> = await sendPost(url, entity, headers);
            setErrors(response.data.errors ?? {});
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? { generic: 'Error' });
            } else if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url, headers]);

    const setEntityProperties = useCallback(
        (properties: Partial<T>) => {
            setEntity({ ...entity, ...properties });
        },
        [entity, setEntity]
    );

    return [entity, setEntityProperties, errors, saving, save] as const;
}

export async function sendGet(url: string, headers: RawAxiosRequestHeaders, params?: {}) {
    return await axios.get(baseUrl + url, {
        headers,
        params
    });
}

export async function sendPost<TInput>(url: string, input: TInput, headers: RawAxiosRequestHeaders, abortController: AbortController | null = null) {
    const options: AxiosRequestConfig<TInput> = {
        headers
    };
    if (abortController !== null) {
        options['signal'] = abortController.signal;
    }
    return await axios.post(baseUrl + url, input, options);
}

export async function sendPut<TInput>(url: string, input: TInput, headers: RawAxiosRequestHeaders) {
    return await axios.put(baseUrl + url, input, {
        headers
    });
}

export function useEntityEdit<TEntity, TInput>(url: string, loadUrl: string, initialValue: (entity: TEntity) => TInput) {
    const [entity, setEntity] = useState<TInput | null>(null);
    const [item, setItem] = useState<TEntity | null>(null);
    const [errors, setErrors] = useState<{ [id: string]: string }>({});
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(false);
    const headers = useDefaultHeaders();
    const { id } = useParams();

    useEffect(() => {
        async function load() {
            if (id) {
                const query: RequestQuery = {
                    language: 'sk',
                    page: 1,
                    pageSize: 1,
                    filters: {
                        id: [id]
                    },
                    requiredFacets: []
                };

                setLoading(true);
                setItem(null);
                try {
                    const response: AxiosResponse<Response<TEntity>> = await sendPost(loadUrl, query, headers);
                    if (response.data.items.length > 0) {
                        setItem(response.data.items[0]);
                        setEntity(initialValue(response.data.items[0]));
                    }
                } catch (err) {
                    if (err instanceof Error) {
                        setErrors({ load: err.message });
                    }
                } finally {
                    setLoading(false);
                }
            }
        }

        load();
    }, [id, loadUrl]);

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<SaveResult> = await sendPut(url, entity, headers);
            setErrors(response.data.errors);
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? { generic: err.message });
            } else if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url, headers]);

    const setEntityProperties = useCallback(
        (properties: Partial<TInput>) => {
            if (entity) {
                setEntity({ ...entity, ...properties });
            }
        },
        [entity, setEntity]
    );

    return [entity, item, loading, setEntityProperties, errors, saving, save] as const;
}

export function useDatasetAdd(initialValue: DatasetInput) {
    return useEntityAdd<DatasetInput>('datasets', initialValue);
}

export function useDatasetEdit(initialValue: (entity: Dataset) => DatasetInput) {
    return useEntityEdit<Dataset, DatasetInput>('datasets', 'datasets/search', initialValue);
}

export function useDistributionAdd(initialValue: DistributionInput) {
    return useEntityAdd<DistributionInput>('distributions', initialValue);
}

export function useDistributionEdit(initialValue: (entity: Distribution) => DistributionInput) {
    return useEntityEdit<Distribution, DistributionInput>('distributions', 'distributions/search', initialValue);
}

export function useLocalCatalogAdd(initialValue: LocalCatalogInput) {
    return useEntityAdd<LocalCatalogInput>('local-catalogs', initialValue);
}

export function useLocalCatalogEdit(initialValue: (entity: LocalCatalog) => LocalCatalogInput) {
    return useEntityEdit<LocalCatalog, LocalCatalogInput>('local-catalogs', 'local-catalogs/search', initialValue);
}

export const supportedLanguages: Language[] = [
    {
        id: 'sk',
        name: 'slovensky',
        nameInPrimaryLanguage: 'slovensky',
        isPrimary: true,
        isRequired: true
    },
    {
        id: 'en',
        name: 'english',
        nameInPrimaryLanguage: 'anglicky',
        isPrimary: false,
        isRequired: false
    }
];

export function useUserInfo() {
    const ctx = useContext(TokenContext);

    return [ctx?.userInfo ?? null, ctx?.userInfoLoading ?? false] as const;
}

export function useDefaultHeaders() {
    const ctx = useContext(TokenContext);
    return ctx?.defaultHeaders ?? {};
}

export function useUserPermissions() {
    const [userInfo] = useUserInfo();
    const userRole = userInfo?.role?.toUpperCase();

    const isLogged = Boolean(userInfo?.id);
    const isSuperAdmin = userRole === 'SUPERADMIN';
    const isCommunityUser = userRole === 'COMMUNITYUSER';
    const isPublisher = ['PUBLISHER', 'PUBLISHERADMIN'].includes(userRole ?? '');
    const isMine = (objectUserId: string) => userInfo?.id === objectUserId;
    const isMineOrg = (objectOrgId: string) => userInfo?.publisher === objectOrgId;

    return {
        isSuperAdmin,
        isCommunityUser,
        isPublisher,
        isLogged,
        isMine,
        isMineOrg
    };
}

export function extractLanguageErrors(errors: { [id: string]: string }, key: string) {
    const filtered: { [id: string]: string } = {};
    for (const [k, v] of Object.entries(errors)) {
        if (k.startsWith(key)) {
            filtered[k.substring(key.length)] = v;
        }
    }
    return filtered;
}

export async function removeEntity(prompt: string, url: string, id: string, headers: RawAxiosRequestHeaders): Promise<boolean | string> {
    if (window.confirm(prompt)) {
        try {
            await axios.delete(baseUrl + url, {
                headers: headers,
                params: {
                    id: id
                }
            });
            return true;
        } catch (err) {
            if (err instanceof AxiosError) {
                return err.response?.data ?? err.message ?? 'Error';
            }
        }
    }
    return false;
}

type FileUploadResult = {
    id: string;
    url: string;
};

export function removeDataset(prompt: string, id: string, headers: RawAxiosRequestHeaders) {
    return removeEntity(prompt, 'datasets', id, headers);
}

export function removeDistribution(prompt: string, id: string, headers: RawAxiosRequestHeaders) {
    return removeEntity(prompt, 'distributions', id, headers);
}

export function removeLocalCatalog(prompt: string, id: string, headers: RawAxiosRequestHeaders) {
    return removeEntity(prompt, 'local-catalogs', id, headers);
}

export function removeUser(prompt: string, id: string, headers: RawAxiosRequestHeaders) {
    return removeEntity(prompt, 'users', id, headers);
}

export function useSingleFileUpload(url: string) {
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();
    const { t } = useTranslation();

    const upload = useCallback(
        async (file: File) => {
            const formData = new FormData();
            formData.append('file', file, file.name);

            if (file.size <= 262144000) {
                setUploading(true);
                setError(null);
                try {
                    const response: AxiosResponse<FileUploadResult> = await axios.post(baseUrl + url, formData, {
                        headers: headers
                    });
                    return response.data;
                } catch (err) {
                    if (axios.isCancel(err)) {
                        return;
                    }
                    if (err instanceof Error) {
                        setError(err);
                    }
                } finally {
                    setUploading(false);
                }
            } else {
                setError(new Error(t('fileSizeExceedLimit')));
            }
        },
        [url, headers, t]
    );

    return [uploading, upload, error] as const;
}

export function useDistributionFileUpload() {
    return useSingleFileUpload('upload');
}

export function useCodelistFileUpload() {
    return useSingleFileUpload('codelists');
}

export function useCodelistAdmin() {
    const [items, setItems] = useState<CodelistAdminView[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const headers = useDefaultHeaders();

    const refresh = useCallback(async () => {
        setLoading(true);
        try {
            const response: AxiosResponse<CodelistAdminView[]> = await axios.post(
                baseUrl + 'codelists/search',
                {},
                {
                    headers: headers
                }
            );
            setItems(response.data);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
        } finally {
            setLoading(false);
        }
    }, [headers]);

    useEffect(() => {
        refresh();
    }, [refresh]);

    return [items, loading, error, refresh] as const;
}

export async function doLogin(headers: RawAxiosRequestHeaders, method?: LoginMethod) {
    type DelegationAuthorizationResult = { redirectUrl: string };
    const params: { [key: string]: string } = {};
    if (method) {
        params['method'] = method;
    }

    const response: AxiosResponse<DelegationAuthorizationResult> = await sendGet('saml/login', headers, params);
    return response.data.redirectUrl;
}

export async function doLogout(headers: RawAxiosRequestHeaders) {
    type DelegationAuthorizationResult = { redirectUrl: string };
    const response: AxiosResponse<DelegationAuthorizationResult> = await sendGet('saml/logout', headers);
    return response.data.redirectUrl;
}

export async function loginWithRedirect(headers: RawAxiosRequestHeaders, method?: LoginMethod) {
    const url = await doLogin(headers, method);
    if (url) {
        window.location.href = url;
    }
}

export function useUserAdd(initialValue: NewUser) {
    return useEntityAdd<NewUser>('users', initialValue);
}

function convertUserToInput(entity: User) {
    return {
        id: entity.id,
        firstName: entity.firstName,
        lastName: entity.lastName,
        email: entity.email,
        role: entity.role
    };
}

export function useUserEdit() {
    const [entity, setEntity] = useState<EditUser | null>(null);
    const [errors, setErrors] = useState<{ [id: string]: string }>({});
    const [saving, setSaving] = useState(false);
    const [item, setItem] = useState<User | null>(null);
    const [loading, setLoading] = useState(false);
    const headers = useDefaultHeaders();
    const { id } = useParams();

    useEffect(() => {
        async function load() {
            if (id) {
                const query = {
                    id: id
                };

                setLoading(true);
                setItem(null);
                try {
                    const response: AxiosResponse<Response<User>> = await sendPost('users/search', query, headers);
                    if (response.data.items.length > 0) {
                        const user = response.data.items[0];
                        setItem(user);
                        setEntity(convertUserToInput(user));
                    }
                } catch (err) {
                    if (err instanceof Error) {
                        setErrors({ load: err.message });
                    }
                } finally {
                    setLoading(false);
                }
            }
        }

        load();
    }, [id]);

    const save = useCallback(async () => {
        setSaving(true);
        setErrors({});
        try {
            const response: AxiosResponse<UserSaveResult> = await sendPut('users', entity, headers);
            setErrors(response.data.errors);
            return response.data;
        } catch (err) {
            if (err instanceof AxiosError) {
                setErrors(err.response?.data.errors ?? { generic: err.message });
            } else if (err instanceof Error) {
                setErrors({
                    generic: err.message
                });
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, headers]);

    const setEntityProperties = useCallback(
        (properties: Partial<EditUser>) => {
            if (entity) {
                setEntity({ ...entity, ...properties });
            }
        },
        [entity, setEntity]
    );

    return [entity, item, loading, setEntityProperties, errors, saving, save] as const;
}

export function useDocumentTitle(text: string) {
    const { t } = useTranslation();

    useEffect(() => {
        document.title = t('nkod') + (text.length > 0 ? ' - ' + text : '');
    }, [text, t]);
}

export function useEndpointUrl() {
    const [endpointUrl, setEndpointUrl] = useState<string | null>(null);

    useEffect(() => {
        async function fetch() {
            const response: AxiosResponse<string> = await sendGet('sparql-endpoint-url', {});
            setEndpointUrl(response.data);
        }
        fetch();
    }, []);

    return endpointUrl;
}

export const useSearchPublisher = ({ language, query }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const { t } = useTranslation();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [publishers, setPublishers] = useState<AutocompleteOption<string>[]>([]);
    const [totalCount, setTotalCount] = useState<number>(0);
    const headers = useDefaultHeaders();

    const load = async (query: string, filters?: any, pageSize = 200) => {
        setLoading(true);
        setError(null);
        try {
            const response = await sendPost(
                'publishers/search',
                {
                    language: language,
                    page: 1,
                    filters,
                    pageSize,
                    orderBy: 'name',
                    queryText: query
                },
                headers
            );
            let data: AutocompleteOption<string>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item: Publisher) => ({
                        value: item.key,
                        label: item?.nameAll?.sk
                    }))
                    .sort((a: AutocompleteOption<string>, b: AutocompleteOption<string>) => a.label.localeCompare(b.label));

                if (response.data.totalCount > data.length) {
                    data.push({ value: MORE_FAKE_OPTION, label: t('common.moreInDB'), isDisabled: true });
                }
            }
            setPublishers(data);
            setTotalCount(response.data.totalCount);
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

    return [publishers, loading, error, load, totalCount] as const;
};

export const useSearchDataset = ({ language, query, filters }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const { t } = useTranslation();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [datasets, setDatasets] = useState<AutocompleteOption<string>[]>([]);
    const headers = useDefaultHeaders();

    const load = async (query: string, filters?: any, pageSize = 200) => {
        setLoading(true);
        setError(null);
        try {
            const response = await sendPost(
                'datasets/search',
                {
                    language,
                    page: 1,
                    filters,
                    pageSize,
                    orderBy: 'name',
                    queryText: query
                },
                headers
            );
            let data: AutocompleteOption<string>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item: Dataset) => ({
                        value: item.key,
                        label: item.name ?? ''
                    }))
                    .sort((a: AutocompleteOption<string>, b: AutocompleteOption<string>) => a.label.localeCompare(b.label));

                if (response.data.totalCount > data.length) {
                    data.push({ value: MORE_FAKE_OPTION, label: t('common.moreInDB'), isDisabled: true });
                }
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

export function usePublisherLists({
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
    const headers = useDefaultHeaders();

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setPublishers([]);
            try {
                const response = await sendPost(
                    'publishers/search',
                    {
                        language: language,
                        page: page,
                        pageSize: pageSize,
                        orderBy: orderBy
                    },
                    headers
                );
                if (response.data.items.length > 0) {
                    setPublishers(
                        response.data.items
                            .map((item: Publisher) => ({
                                value: item,
                                label: item?.nameAll?.sk
                            }))
                            .sort((a: AutocompleteOption<Publisher>, b: AutocompleteOption<Publisher>) => a.label.localeCompare(b.label))
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

function concatErrors(errors: { [id: string]: string }) {
    return Object.keys(errors)?.map((errorKey) => new Error(errors[errorKey]));
}
