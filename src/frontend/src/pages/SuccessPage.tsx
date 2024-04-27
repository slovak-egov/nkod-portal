import { ReactNode } from 'react';
import Alert from '../components/Alert';
import Button from '../components/Button';

type Props = {
    msg: ReactNode;
    backButtonLabel: string;
    backButtonClick: () => void;
};

export default function SuccessPage(props: Props) {
    const { msg, backButtonLabel, backButtonClick } = props;
    return (
        <>
            <Alert type={'info'}>
                <div className="govuk-!-padding-4">
                    <span className="govuk-heading-m govuk-!-font-weight-bold">{msg}</span>
                    <div>
                        <Button buttonType="secondary" onClick={backButtonClick}>
                            {backButtonLabel}
                        </Button>
                    </div>
                </div>
            </Alert>
        </>
    );
}
