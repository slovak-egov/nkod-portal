import React from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';
import Select from 'react-select/async';

import { CodelistValue, sendPost, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import { AutocompleteOption, SuggestionFormValues, SuggestionType, useSearchDataset, useSearchPublisher } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import FormElementGroup from '../components/FormElementGroup';
import SelectElementItems from '../components/SelectElementItems';
import BaseInput from '../components/BaseInput';
import TextArea from '../components/TextArea';
import Button from '../components/Button';


export const suggestionTypeCodeList: CodelistValue[] = [
    {
        id: SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET,
        label: 'podnet na zverejnenie nového datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET,
        label: 'podnet na kvalitu dát zverejneného datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_METADATA,
        label: 'podnet na kvalitu metadát zverejneného datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_OTHER,
        label: 'iný podnet'
    }
];

const AddSuggestion = () => {
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('addApplicationPage.headerTitle'));
    // const tokenContext = useContext(TokenContext);

    const [publisherList, loadingPublisherList, errorPublisherList, searchPublisher] = useSearchPublisher({
        language: 'sk', query: ''
    });
    const [datasetList, loadingDatasetList, errorDatasetList, searchDataset] = useSearchDataset({
        language: 'sk', query: ''
    });

    const { control, register, handleSubmit, watch, formState: { errors } } = useForm<SuggestionFormValues>();

    const onSubmit: SubmitHandler<SuggestionFormValues> = async (data) => {
        const result = await sendPost<SuggestionFormValues>(`/api/suggestion`, data, headers);
        // setSaveResult(result.data);
    };

    const loadOrganizationOptions = (inputValue: string, callback: (options: AutocompleteOption[]) => void) => {
        searchPublisher(inputValue).then((options) => {
            console.log('options: ', options);
            return callback(options || []);
        });
    };

    const loadDatasetOptions = (inputValue: string, callback: (options: AutocompleteOption[]) => void) => {
        searchDataset(inputValue).then((options) => {
            console.log('options: ', options);
            return callback(options || []);
        });
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('addSuggestion.headerTitle') }]} />
            <MainContent>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <PageHeader>{t('addSuggestion.title')}</PageHeader>

                    <p>
                        {t('addSuggestion.userTitle', { name: `${userInfo?.firstName} ${userInfo?.lastName}` })}
                    </p>
                    <p>
                        {t('addSuggestion.organizationTitle', { name: userInfo?.companyName })}
                    </p>

                    <h2 className='govuk-heading-m '>
                        {t('addSuggestion.subtitle')}
                    </h2>

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('addSuggestion.fields.organization')}
                                element={id => <Select
                                    id={id}
                                    styles={{
                                        control: (provided, state) => (state.isFocused ? {
                                            ...provided,
                                            outline: '3px solid #ffdf0f!important',
                                            borderColor: '#0b0c0c!important',
                                            outlineOffset: '0!important',
                                            boxShadow: 'inset 0 0 0 2px!important'
                                        } : {
                                            ...provided,
                                            border: '2px solid #0b0c0c'
                                        })
                                    }}
                                    isClearable
                                    components={{ IndicatorSeparator: () => null }}
                                    loadingMessage={t('searchAutocomplete.loading')}
                                    noOptionsMessage={t('searchAutocomplete.noResults')}
                                    placeholder={t('searchAutocomplete.placeholder')}
                                    isLoading={loadingPublisherList}
                                    loadOptions={loadOrganizationOptions}
                                    value={publisherList.find(x => x.value === field.value)}
                                    getOptionLabel={x => x.label}
                                    onChange={x => field.onChange(x?.value)}
                                    defaultOptions={publisherList}
                                />}
                            />
                        )}
                        name='organization'
                        control={control}
                    />

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('addSuggestion.fields.dataset')}
                                element={id => <Select
                                    id={id}
                                    styles={{
                                        control: (provided, state) => (state.isFocused ? {
                                            ...provided,
                                            outline: '3px solid #ffdf0f!important',
                                            borderColor: '#0b0c0c!important',
                                            outlineOffset: '0!important',
                                            boxShadow: 'inset 0 0 0 2px!important'
                                        } : {
                                            ...provided,
                                            border: '2px solid #0b0c0c'
                                        })
                                    }}
                                    isClearable
                                    components={{ IndicatorSeparator: () => null }}
                                    loadingMessage={t('searchAutocomplete.loading')}
                                    noOptionsMessage={t('searchAutocomplete.noResults')}
                                    placeholder={t('searchAutocomplete.placeholder')}
                                    isLoading={loadingDatasetList}
                                    loadOptions={loadDatasetOptions}
                                    value={datasetList.find(x => x.value === field.value)}
                                    getOptionLabel={x => x.label}
                                    onChange={x => field.onChange(x?.value)}
                                    defaultOptions={datasetList}
                                />}
                            />
                        )}
                        name='dataset'
                        control={control}
                    />

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('addSuggestion.fields.suggestionType')}
                                element={id => <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={false}
                                    options={suggestionTypeCodeList}
                                    selectedValue={field.value}
                                    onChange={field.onChange}
                                    renderOption={v => v.label}
                                    getValue={v => v.id}
                                />}
                            />
                        )}
                        name='suggestionType'
                        control={control}
                    />

                    <FormElementGroup
                        label={t('addSuggestion.fields.suggestionTitle')}
                        element={id =>
                            <BaseInput
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                                placeholder='Výber'
                            />
                        }
                    />

                    <FormElementGroup
                        label={t('addSuggestion.fields.suggestionDescription')}
                        element={id =>
                            <TextArea
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                            />
                        }
                    />

                    <Button>
                        {t('addSuggestion.addButton')}
                    </Button>

                </form>
            </MainContent>
        </>
    );
};

export default AddSuggestion;
