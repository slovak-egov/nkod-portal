import { yupResolver } from '@hookform/resolvers/yup';
import { useEffect, useRef } from 'react';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { buildYup } from 'schema-to-yup';
import { CodelistValue, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import {
    AutocompleteOption,
    Suggestion,
    SuggestionFormValues,
    SuggestionStatusCode,
    SuggestionType,
    sendPost,
    sendPut,
    useSearchDataset,
    useSearchPublisher
} from '../cms';
import BaseInput from '../components/BaseInput';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import ReactSelectElement from '../components/ReactSelectElement';
import SelectElementItems from '../components/SelectElementItems';
import TextArea from '../components/TextArea';
import { QueryGuard, useLoadData } from '../helpers/helpers';
import CommentSection from './CommentSection';
import { schema, schemaConfig } from './schemas/SuggestionSchema';

// TODO:
// FAKE userInfo.id?
// FAKE userInfo companyURI odkiaľ?
// dokončiť preklady
// disabled select font color
// dataset clear zostáva visieť asi useLoadData FormData robí zle
// ? zobrazenie chýb nejako inak ako v konzole
// ? Vymyslieť spôsob na parsovanie JSON schema (možno lib)

export const suggestionTypeCodeList: CodelistValue[] = [
    {
        id: SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET,
        label: 'podnet na zverejnenie nového datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET,
        label: 'podnet na kvalitu dát zverejneného datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_METADATA,
        label: 'podnet na kvalitu metadát zverejneného datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_OTHER,
        label: 'iný podnet'
    }
];

const suggestionStatusList: CodelistValue[] = [
    {
        id: SuggestionStatusCode.CREATED,
        label: 'zaevidovaný'
    },
    {
        id: SuggestionStatusCode.IN_PROGRESS,
        label: 'v riešení'
    },
    {
        id: SuggestionStatusCode.RESOLVED,
        label: 'vyriešený'
    }
];

type Props = {
    readonly?: boolean;
    scrollToComments?: boolean;
};

export default function SuggestionDetail(props: Props) {
    const headers = useDefaultHeaders();
    const [userInfo] = useUserInfo();
    const datasetSelectRef = useRef(null);
    const commentsRef = useRef(null);
    const { readonly, scrollToComments } = props;
    const { id } = useParams();
    const navigate = useNavigate();
    const { t } = useTranslation();
    const yupSchema = buildYup(schema, schemaConfig);

    useDocumentTitle(t('addApplicationPage.headerTitle'));

    useEffect(() => {
        if (scrollToComments) {
            (commentsRef.current as any)?.scrollIntoView();
        }
    }, []);

    const [publisherList, loadingPublisherList, errorPublisherList, searchPublisher] = useSearchPublisher({
        language: 'sk',
        query: ''
    });
    const [datasetList, loadingDatasetList, errorDatasetList, searchDataset] = useSearchDataset({
        language: 'sk',
        query: ''
    });

    const form = useForm<SuggestionFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b2ae87',
            orgToUri: userInfo?.companyURI,
            status: SuggestionStatusCode.CREATED,
            type: SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET
        }
    });

    const {
        control,
        register,
        handleSubmit,
        watch,
        setValue,
        formState: { errors }
    } = form;

    const loadFormData = useLoadData<any, Suggestion>({
        disabled: !id,
        form,
        url: `cms/suggestions/${id}`,
        transform: async (data: Suggestion) => {
            const pubItems = await searchPublisher('', { key: [data.orgToUri] }, 1);
            const datasetItems = await searchDataset('', { publishers: [data.orgToUri] }, 9999);
            let orgToUri = '';
            let datasetUri = '';
            if (pubItems?.length) {
                orgToUri = pubItems[0]?.value;
            }
            if (datasetItems?.length) {
                datasetUri = data.datasetUri;
            }

            const formData = {
                ...data,
                orgToUri,
                datasetUri
            };
            return formData;
        }
    });

    const onSubmit: SubmitHandler<SuggestionFormValues> = async (data) => {
        let result = null;
        if (id) {
            result = await sendPut<any>(`cms/suggestions/${id}`, data);
        } else {
            result = await sendPost<any>(`cms/suggestions`, data);
        }

        if (result?.status === 200) {
            navigate('/odkomunita', { state: { info: t(id ? 'suggestionEditSuccessful' : 'suggestionAddSuccessful') } });
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    const loadOrganizationOptions = (inputValue: string, callback: (options: AutocompleteOption<any>[]) => void) => {
        searchPublisher(inputValue).then((options) => {
            return callback(options || []);
        });
    };

    const loadDatasetOptions = (inputValue: string, callback: (options: AutocompleteOption<any>[]) => void) => {
        const org = publisherList.find((x) => x.value === form.getValues('orgToUri'));
        if (org?.value) {
            searchDataset(inputValue, { publishers: [org?.value] }).then((options) => {
                return callback(options || []);
            });
        }
    };

    return (
        <>
            <Breadcrumbs
                items={[
                    { title: t('nkod'), link: '/' },
                    { title: t('suggestionList.headerTitle'), link: '/podnet' },
                    { title: t('addSuggestion.headerTitle') }
                ]}
            />
            <QueryGuard {...loadFormData} isNew={!id}>
                <MainContent>
                    {/* {watch() && JSON.stringify(form.getValues())} */}
                    <PageHeader>{t(`addSuggestion.title${id ? 'Edit' : ''}`)}</PageHeader>
                    <GridRow>
                        <GridColumn widthUnits={2} totalUnits={3}>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <GridRow className="govuk-!-margin-bottom-6">
                                    <GridColumn widthUnits={1} totalUnits={3} className="govuk-body-m">
                                        {t('addSuggestion.userTitle')}
                                    </GridColumn>
                                    <GridColumn widthUnits={1} totalUnits={3} className="govuk-body-m govuk-!-font-weight-bold">
                                        {userInfo?.firstName} {userInfo?.lastName}
                                    </GridColumn>
                                </GridRow>

                                <GridRow className="govuk-!-margin-bottom-6">
                                    <GridColumn widthUnits={1} totalUnits={3} className="govuk-body-m">
                                        {t('addSuggestion.organizationTitle')}
                                    </GridColumn>
                                    <GridColumn widthUnits={1} totalUnits={3} className="govuk-body-m govuk-!-font-weight-bold">
                                        {userInfo?.companyName}
                                    </GridColumn>
                                </GridRow>

                                <h2 className="govuk-heading-m govuk-!-margin-bottom-6 suggestion-subtitle">{t('addSuggestion.subtitle')}</h2>

                                <Controller
                                    render={({ field: { onChange, value } }) => (
                                        <FormElementGroup
                                            label={t('addSuggestion.fields.orgToUri')}
                                            element={(id) => (
                                                <ReactSelectElement
                                                    id={id}
                                                    isLoading={loadingPublisherList}
                                                    loadOptions={loadOrganizationOptions}
                                                    value={publisherList.find((x) => x.value === value)}
                                                    disabled={readonly}
                                                    getOptionLabel={(x: any) => {
                                                        return x.label;
                                                    }}
                                                    onChange={(newOption) => {
                                                        onChange(newOption?.value);
                                                        setValue('datasetUri', null);
                                                        (datasetSelectRef.current as any)?.onChange?.(undefined, { action: 'clear' });

                                                        if (newOption?.value) {
                                                            searchDataset('', { publishers: [newOption.value] }, 50);
                                                        }
                                                    }}
                                                    options={publisherList}
                                                />
                                            )}
                                        />
                                    )}
                                    name="orgToUri"
                                    control={control}
                                />
                                <Controller
                                    render={({ field }) => (
                                        <FormElementGroup
                                            label={t('addSuggestion.fields.type')}
                                            element={(id) => (
                                                <SelectElementItems<CodelistValue>
                                                    id={id}
                                                    className={'suggestion-select'}
                                                    disabled={readonly}
                                                    options={suggestionTypeCodeList}
                                                    selectedValue={field.value}
                                                    onChange={(newOption) => {
                                                        field.onChange(newOption);
                                                        if (newOption === SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET) {
                                                            setValue('datasetUri', null);
                                                            (datasetSelectRef.current as any)?.onChange?.(undefined, { action: 'clear' });
                                                        }
                                                    }}
                                                    renderOption={(v) => v.label}
                                                    getValue={(v) => v.id}
                                                />
                                            )}
                                        />
                                    )}
                                    name="type"
                                    control={control}
                                />

                                {watch('type') !== SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET && (
                                    <Controller
                                        render={({ field }) => (
                                            <FormElementGroup
                                                label={t('addSuggestion.fields.datasetUri')}
                                                element={(id) => (
                                                    <ReactSelectElement
                                                        id={id}
                                                        ref={datasetSelectRef}
                                                        disabled={!watch('orgToUri') || readonly}
                                                        isLoading={loadingDatasetList}
                                                        loadOptions={loadDatasetOptions}
                                                        value={datasetList.find((x) => x.value === field.value)}
                                                        getOptionLabel={(x: any) => x.label}
                                                        onChange={(x) => field.onChange(x?.value)}
                                                        options={datasetList}
                                                    />
                                                )}
                                            />
                                        )}
                                        name="datasetUri"
                                        control={control}
                                    />
                                )}

                                <FormElementGroup
                                    label={t('addSuggestion.fields.title')}
                                    element={(id) => <BaseInput id={id} readOnly={readonly} {...register('title')} />}
                                />

                                <FormElementGroup
                                    label={t('addSuggestion.fields.description')}
                                    element={(id) => <TextArea id={id} rows={6} readOnly={readonly} {...register('description')} />}
                                />

                                {id && (
                                    <Controller
                                        render={({ field }) => (
                                            <FormElementGroup
                                                label={t('addSuggestion.fields.state')}
                                                element={(id) => (
                                                    <SelectElementItems<CodelistValue>
                                                        id={id}
                                                        disabled={readonly}
                                                        className={'suggestion-select'}
                                                        options={suggestionStatusList}
                                                        selectedValue={field.value}
                                                        onChange={field.onChange}
                                                        renderOption={(v) => v.label}
                                                        getValue={(v) => v.id}
                                                    />
                                                )}
                                            />
                                        )}
                                        name="status"
                                        control={control}
                                    />
                                )}

                                {!readonly && <Button>{t('addSuggestion.addButton')}</Button>}
                            </form>
                        </GridColumn>
                    </GridRow>
                </MainContent>
            </QueryGuard>
            <div ref={commentsRef}>{id && <CommentSection contentId={id} />}</div>
        </>
    );
}

SuggestionDetail.defaultProps = {
    readonly: false
};
