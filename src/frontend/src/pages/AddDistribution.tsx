import { useDistributionAdd, useDocumentTitle, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import { DistributionForm } from "../components/DistributionForm";
import { useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";


export default function AddDistribution()
{
    const { datasetId } = useParams();

    const [distribution, setDistribution, errors, saving, save] = useDistributionAdd({
        datasetId: datasetId ?? '',
        authorsWorkType: 'https://creativecommons.org/licenses/by/4.0/',
        originalDatabaseType: 'https://creativecommons.org/licenses/by/4.0/',
        databaseProtectedBySpecialRightsType: 'https://creativecommons.org/publicdomain/zero/1.0/',
        personalDataContainmentType: 'https://data.gov.sk/def/personal-data-occurence-type/2',
        downloadUrl: null,
        format: 'http://publications.europa.eu/resource/authority/file-type/CSV',
        mediaType: 'http://www.iana.org/assignments/media-types/text/csv',
        compressFormat: null,
        packageFormat: null,
        conformsTo: null,
        title: null,
        fileId: null
    });

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('newDistribution'));

    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('distributionList'), link: '/sprava/distribucie/' + datasetId}, {title: t('newDistribution')}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('newDistribution')}</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    <DistributionForm distribution={distribution} 
                                      setDistribution={setDistribution} 
                                      errors={errors}
                                      saving={saving} />

                    <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/distribucie/' + datasetId);
                        }
                    }} disabled={saving}>
                        {t('save')}
                    </Button>
                </div>
            </MainContent>
        </>;
}