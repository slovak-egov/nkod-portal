import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import { PublisherInput, useDocumentTitle, useEntityAdd } from "../client";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { ProfileFormControls } from '../components/ProfileFormControls';

export default function PublisherRegistration()
{
    const [publisher, setPublisher, errors, saving, save] = useEntityAdd<PublisherInput>('registration', {
        website: '',
        email: '',
        phone: '',
        legalForm: 'https://data.gov.sk/def/legal-form-type/321'
    });
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('publisherRegistration'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publisherRegistration') }]} />
            <MainContent>
                <PageHeader>{t('publisherRegistration')}</PageHeader>

                <ProfileFormControls publisher={publisher} setPublisher={setPublisher} errors={errors} saving={saving} />

                <Button
                    style={{ marginRight: '20px' }}
                    onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/caka-na-schvalenie');
                        }
                    }}
                    disabled={saving}
                >
                    {t('register')}
                </Button>
            </MainContent>
        </>
    );
}