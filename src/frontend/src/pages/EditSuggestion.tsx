import React, { useContext } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';

import { CodelistValue, sendPost, TokenContext, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import {
    AutocompleteOption,
    EditSuggestionFormValues,
    SuggestionStatusCode,
    SuggestionType,
    useSearchDataset,
    useSearchPublisher
} from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import FormElementGroup from '../components/FormElementGroup';
import SelectElementItems from '../components/SelectElementItems';
import BaseInput from '../components/BaseInput';
import TextArea from '../components/TextArea';
import Button from '../components/Button';
import { suggestionTypeCodeList } from './AddSuggestion';
import Select from 'react-select/async';

const EditSuggestion = () => {
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('editSuggestion.headerTitle'));
    const tokenContext = useContext(TokenContext);


    const [publisherList, loadingPublisherList, errorPublisherList, searchPublisher] = useSearchPublisher({
        language: 'sk', query: ''
    });
    const [datasetList, loadingDatasetList, errorDatasetList, searchDataset] = useSearchDataset({
        language: 'sk', query: ''
    });

    const { control, register, handleSubmit, watch, formState: { errors } } = useForm<EditSuggestionFormValues>();

    const onSubmit: SubmitHandler<EditSuggestionFormValues> = async (data) => {
        const result = await sendPost<EditSuggestionFormValues>(`/api/suggestion`, data, headers);
        // setSaveResult(result.data);
    };


    const suggestionStatusList: CodelistValue[] = [
        {
            id: SuggestionStatusCode.PROPOSAL_FOR_CHANGE,
            label: 'Návrh na zmenu'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_FOR_CREATIOM,
            label: 'Návrh na vytvorenie'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_REJECTED,
            label: 'Návrh zamietnutý'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_APPROVED,
            label: 'Návrh schválený'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_IN_PROGRESS,
            label: 'Návrh v riešení'
        }
    ];

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
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('editSuggestion.headerTitle') }]} />
            <MainContent>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <PageHeader>{t('editSuggestion.title', {name: 'Zverejnenie CRZ cez OpenDataAPI'})}</PageHeader>

                    <p>
                        {t('editSuggestion.userTitle', { name: `${userInfo?.firstName} ${userInfo?.lastName}` })}
                    </p>
                    <p>
                        {t('editSuggestion.organizationTitle', { name: userInfo?.companyName })}
                    </p>

                    <h2 className='govuk-heading-m '>
                        {t('editSuggestion.subtitle')}
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
                                    components={{IndicatorSeparator: () => null }}
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
                                    components={{IndicatorSeparator: () => null }}
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
                                label={t('editSuggestion.fields.suggestionType')}
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
                        label={t('editSuggestion.fields.suggestionTitle')}
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
                        label={t('editSuggestion.fields.suggestionDescription')}
                        element={id =>
                            <TextArea
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                            />
                        }
                    />


                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('editSuggestion.fields.suggestionStatus')}
                                element={id => <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={false}
                                    options={suggestionStatusList}
                                    selectedValue={field.value}
                                    onChange={field.onChange}
                                    renderOption={v => v.label}
                                    getValue={v => v.id}
                                />}
                            />
                        )}
                        name='suggestionStatus'
                        control={control}
                    />

                    <Button >
                        {t('editSuggestion.addButton')}
                    </Button>
                </form>
                <a className="govuk-link" title={t('idsk')} href="https://idsk.gov.sk/">
                    {t('editSuggestion.commentButton')}
                </a>.
            </MainContent>
        </>
    );
};

export default EditSuggestion;
