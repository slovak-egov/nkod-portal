import { Dataset } from '../client';

export type AppRegistrationFormValues = {
    title: string;
    userId: string;
    userEmail: string;
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
    userId: string;
    userEmail: string;
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
    userId: string;
    userEmail: string;
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
