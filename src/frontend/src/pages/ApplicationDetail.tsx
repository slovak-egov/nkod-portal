import moment from 'moment';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useDocumentTitle } from '../client';
import { useCmsApplication } from '../cms';
import { applicationThemeCodeList, applicationTypeCodeList } from '../codelist/ApplicationCodelist';
import Breadcrumbs from '../components/Breadcrumbs';
import GridRow from '../components/GridRow';
import Loading from '../components/Loading';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { DATE_FORMAT_NO_SECONDS } from '../helpers/helpers';
import CommentSection from './CommentSection';
import DetailItemElement from './DetailItemElement';

export default function ApplicationDetail() {
    const { id } = useParams();
    const [application, loading] = useCmsApplication(id);
    const { t } = useTranslation();
    useDocumentTitle(application?.title ?? '');

    return (
        <>
            {loading ? (
                <Loading />
            ) : (
                application && (
                    <>
                        <Breadcrumbs
                            items={[
                                { title: t('nkod'), link: '/' },
                                { title: t('suggestionList.headerTitle'), link: '/aplikacia' },
                                { title: application.title }
                            ]}
                        />
                        <MainContent>
                            <div className="nkod-entity-detail">
                                <PageHeader>{application.title}</PageHeader>
                                <GridRow>
                                    <DetailItemElement value={application.description} labelKey="" />

                                    <DetailItemElement
                                        value={applicationTypeCodeList.find((a) => a.id === application.type)?.label ?? ''}
                                        labelKey="addApplicationPage.fields.applicationType"
                                    />

                                    <DetailItemElement
                                        value={applicationThemeCodeList.find((a) => a.id === application.theme)?.label ?? ''}
                                        labelKey="addApplicationPage.fields.applicationTheme"
                                    />

                                    <DetailItemElement
                                        value={
                                            <a href={application.url} className="govuk-link">
                                                {application.url}
                                            </a>
                                        }
                                        labelKey="addApplicationPage.fields.applicationUrl"
                                    />

                                    {application?.datasetURIs?.length && (
                                        <DetailItemElement
                                            value={application?.datasetURIs?.map((d) => (
                                                <p className="govuk-!-margin-0">
                                                    <a href={d} className="govuk-link">
                                                        {d}
                                                    </a>
                                                </p>
                                            ))}
                                            labelKey="addApplicationPage.fields.applicationDataset"
                                        />
                                    )}

                                    {application?.logo && (
                                        <DetailItemElement
                                            value={<img src={application.logo} width="200px" alt={t('addApplicationPage.fields.applicationLogo')} />}
                                            labelKey="addApplicationPage.fields.applicationLogo"
                                        />
                                    )}

                                    <DetailItemElement
                                        value={`${application.contactName} ${application.contactSurname} (${application.contactEmail})`}
                                        labelKey="addApplicationPage.contactSubTitle"
                                    />

                                    <DetailItemElement value={application.userId} labelKey="common.user" />

                                    <DetailItemElement
                                        value={moment(application.created).format(DATE_FORMAT_NO_SECONDS)}
                                        labelKey="addSuggestion.fields.created"
                                    />

                                    <DetailItemElement
                                        value={moment(application.updated).format(DATE_FORMAT_NO_SECONDS)}
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
