import { AdminPublisherInput, useDocumentTitle, useEntityAdd } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { AdminPublisherForm } from '../components/AdminPublisherForm';

export default function AddPublisher() {
    const [publisher, setPublisher, errors, saving, save] = useEntityAdd<AdminPublisherInput>('publishers', {
        id: null,
        name: { sk: '' },
        website: '',
        email: '',
        phone: '',
        legalForm: 'https://data.gov.sk/def/legal-form-type/321',
        uri: 'https://data.gov.sk/id/legal-subject/',
        isEnabled: false
    });

    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('newPublisher'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publishers'), link: '/sprava/poskytovatelia' }, { title: t('newPublisher') }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('newPublisher')}</PageHeader>

                    {Object.keys(errors).length > 0 ? (
                        <ValidationSummary
                            elements={Object.entries(errors).map((k) => ({
                                elementId: k[0],
                                message: k[1]
                            }))}
                        />
                    ) : null}

                    <AdminPublisherForm publisher={publisher} setPublisher={setPublisher} errors={errors} saving={saving} />

                    <Button
                        style={{ marginRight: '20px' }}
                        onClick={async () => {
                            const result = await save();
                            if (result?.success) {
                                navigate('/sprava/poskytovatelia');
                            }
                        }}
                        disabled={saving}
                    >
                        {t('save')}
                    </Button>
                </div>
            </MainContent>
        </>
    );
}
