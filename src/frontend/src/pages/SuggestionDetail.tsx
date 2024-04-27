import moment from 'moment';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useDocumentTitle } from '../client';
import { useCmsSuggestion } from '../cms';
import { suggestionStatusList, suggestionTypeCodeList } from '../codelist/SuggestionCodelist';
import Breadcrumbs from '../components/Breadcrumbs';
import GridRow from '../components/GridRow';
import Loading from '../components/Loading';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { DATE_FORMAT_NO_SECONDS } from '../helpers/helpers';
import CommentSection from './CommentSection';
import DetailItemElement from './DetailItemElement';

export default function SuggestionDetail() {
    const { id } = useParams();
    const [suggestion, loading] = useCmsSuggestion(id);
    const { t } = useTranslation();
    useDocumentTitle(suggestion?.title ?? '');

    return (
        <>
            {loading ? (
                <Loading />
            ) : (
                suggestion && (
                    <>
                        <Breadcrumbs
                            items={[{ title: t('nkod'), link: '/' }, { title: t('suggestionList.headerTitle'), link: '/podnet' }, { title: suggestion.title }]}
                        />
                        <MainContent>
                            <div className="nkod-entity-detail">
                                <PageHeader>{suggestion.title}</PageHeader>
                                <GridRow>
                                    <DetailItemElement value={suggestion.description} labelKey="" />

                                    <DetailItemElement
                                        value={
                                            <a href={suggestion.orgToUri} className="govuk-link">
                                                {suggestion.orgName}
                                            </a>
                                        }
                                        labelKey="addSuggestion.fields.orgToUriSingle"
                                    />

                                    <DetailItemElement
                                        value={suggestionTypeCodeList.find((s) => s.id === suggestion.type)?.label ?? ''}
                                        labelKey="addSuggestion.fields.type"
                                    />

                                    <DetailItemElement
                                        value={
                                            <a href={suggestion.datasetUri} className="govuk-link">
                                                {suggestion.datasetName ?? suggestion.datasetUri}
                                            </a>
                                        }
                                        labelKey="addSuggestion.fields.datasetUri"
                                    />

                                    <DetailItemElement
                                        value={suggestionStatusList.find((s) => s.id === suggestion.status)?.label ?? ''}
                                        labelKey="addSuggestion.fields.status"
                                    />

                                    <DetailItemElement value={suggestion.userId} labelKey="common.user" />

                                    {suggestion.userOrgUri && <DetailItemElement value={suggestion.userOrgUri} labelKey="common.userOrgUri" />}

                                    <DetailItemElement
                                        value={moment(suggestion.created).format(DATE_FORMAT_NO_SECONDS)}
                                        labelKey="addSuggestion.fields.created"
                                    />

                                    <DetailItemElement
                                        value={moment(suggestion.updated).format(DATE_FORMAT_NO_SECONDS)}
                                        labelKey="addSuggestion.fields.updated"
                                    />
                                </GridRow>
                            </div>
                        </MainContent>
                    </>
                )
            )}
            {id && <CommentSection contentId={id} />}
        </>
    );
}
