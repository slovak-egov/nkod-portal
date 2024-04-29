import { yupResolver } from '@hookform/resolvers/yup';
import { useCallback, useRef, useState } from 'react';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { buildYup } from 'schema-to-yup';
import { CodelistValue, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import { sendDelete, sendPost, sendPut, useSearchDataset, useSearchPublisher } from '../cms';
import { suggestionStatusList, suggestionTypeCodeList } from '../codelist/SuggestionCodelist';
import BaseInput from '../components/BaseInput';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import ReactSelectElement, { AutocompleteOption } from '../components/ReactSelectElement';
import SelectElementItems from '../components/SelectElementItems';
import TextArea from '../components/TextArea';
import { QueryGuard, useLoadData, useSchemaConfig } from '../helpers/helpers';
import { Suggestion, SuggestionFormValues, SuggestionStatusCode, SuggestionType } from '../interface/cms.interface';
import CommentSection from './CommentSection';
import SuccessErrorPage from './SuccessErrorPage';
import { schema } from './schemas/SuggestionSchema';

export default function SuggestionForm() {
    const [saveSuccess, setSaveSuccess] = useState<boolean>(false);
    const headers = useDefaultHeaders();
    const [userInfo] = useUserInfo();
    const datasetSelectRef = useRef(null);
    const { id } = useParams();
    const navigate = useNavigate();
    const { t } = useTranslation();
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));

    useDocumentTitle(t('addApplicationPage.headerTitle'));

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

    const deleteSuggestion = useCallback(async () => {
        const result = await sendDelete(`cms/suggestions/${id}`);
        if (result?.status === 200) {
            navigate('/podnet');
        }
    }, [id, navigate]);

    const onSubmit: SubmitHandler<SuggestionFormValues> = async (data) => {
        let result = null;
        if (id) {
            result = await sendPut<any>(`cms/suggestions/${id}`, data);
        } else {
            result = await sendPost<any>(`cms/suggestions`, data);
        }

        if (result?.status === 200) {
            setSaveSuccess(true);
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
            {saveSuccess ? (
                <SuccessErrorPage
                    msg={id ? t('suggestionEditSuccessful') : t('suggestionAddSuccessful')}
                    backButtonLabel={t('common.backToList')}
                    backButtonClick={() => navigate('/podnet')}
                />
            ) : (
                <>
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

                                        <h2 className="govuk-heading-m govuk-!-margin-bottom-6 suggestion-subtitle">{t('addSuggestion.subtitle')}</h2>

                                        <Controller
                                            rules={{ required: '' }}
                                            render={({ field: { onChange, value } }) => (
                                                <FormElementGroup
                                                    label={t('addSuggestion.fields.orgToUri')}
                                                    errorMessage={errors.orgToUri?.message as string}
                                                    element={(id) => (
                                                        <ReactSelectElement
                                                            id={id}
                                                            isLoading={loadingPublisherList}
                                                            loadOptions={loadOrganizationOptions}
                                                            value={publisherList.find((x) => x.value === value)}
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
                                                rules={{ required: '' }}
                                                render={({ field }) => (
                                                    <FormElementGroup
                                                        label={t('addSuggestion.fields.datasetUri')}
                                                        errorMessage={errors.datasetUri?.message as string}
                                                        element={(id) => (
                                                            <ReactSelectElement
                                                                id={id}
                                                                ref={datasetSelectRef}
                                                                disabled={!watch('orgToUri')}
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
                                            element={(id) => <BaseInput id={id} {...register('title')} />}
                                            errorMessage={errors.title?.message}
                                        />

                                        <FormElementGroup
                                            label={t('addSuggestion.fields.description')}
                                            element={(id) => <TextArea id={id} rows={6} {...register('description')} />}
                                            errorMessage={errors.description?.message}
                                        />

                                        {id && (
                                            <Controller
                                                render={({ field }) => (
                                                    <FormElementGroup
                                                        label={t('addSuggestion.fields.status')}
                                                        element={(id) => (
                                                            <SelectElementItems<CodelistValue>
                                                                id={id}
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

                                        <GridRow>
                                            <GridColumn widthUnits={1} totalUnits={2}>
                                                <Button type={'submit'}>{t('addSuggestion.addButton')}</Button>
                                            </GridColumn>
                                            {id && (
                                                <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                                    <Button buttonType="warning" type={'button'} onClick={deleteSuggestion}>
                                                        {t('common.delete')}
                                                    </Button>
                                                </GridColumn>
                                            )}
                                        </GridRow>
                                    </form>
                                </GridColumn>
                            </GridRow>
                        </MainContent>
                    </QueryGuard>
                    {id && <CommentSection contentId={id} />}
                </>
            )}
        </>
    );
}
